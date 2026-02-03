using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CommunityShareStack.Services
{
    public class OpenAiVisionClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiVisionClient> _logger;

        public OpenAiVisionClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiVisionClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BookExtractionResult> ExtractBookAsync(List<string> imagePaths)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var request = BuildRequest(model, imagePaths);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI error: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {Trim(body, 800)}");
            }

            var extractedJson = ExtractJsonFromResponse(body);
            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                throw new InvalidOperationException("OpenAI returned an empty response.");
            }

            var result = JsonSerializer.Deserialize<BookExtractionResult>(extractedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (result == null)
            {
                throw new InvalidOperationException("OpenAI returned invalid JSON.");
            }

            result.RawJson = extractedJson;
            return result;
        }

        public async Task<string> ExtractOcrTextAsync(List<string> imagePaths)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured.");
            }

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var request = BuildOcrRequest(model, imagePaths);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI OCR error: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"OpenAI OCR request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {Trim(body, 800)}");
            }

            return ExtractTextFromResponse(body);
        }

        public static string TryFindIsbnFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var cleaned = text.Replace("-", "").Replace(" ", "");
            var isbn13 = Regex.Match(cleaned, @"97[89]\d{10}");
            if (isbn13.Success)
            {
                return isbn13.Value;
            }

            var isbn10 = Regex.Match(cleaned, @"\b\d{9}[0-9X]\b");
            return isbn10.Success ? isbn10.Value : null;
        }

        private static object BuildRequest(string model, List<string> imagePaths)
        {
            var contentParts = new List<object>
            {
                new
                {
                    type = "input_text",
                    text = "Extract book metadata from the images. Use the schema exactly. Prefer ISBN-13 when available."
                }
            };

            foreach (var path in imagePaths)
            {
                var bytes = File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:image/jpeg;base64,{base64}";
                contentParts.Add(new
                {
                    type = "input_image",
                    image_url = dataUrl
                });
            }

            return new
            {
                model,
                input = new[]
                {
                    new
                    {
                        role = "user",
                        content = contentParts
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "book_extraction",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                title = new { type = "string" },
                                subtitle = new { type = "string" },
                                authors = new { type = "array", items = new { type = "string" } },
                                isbn_candidates = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        properties = new
                                        {
                                            value = new { type = "string" },
                                            confidence = new { type = "number" }
                                        },
                                        required = new[] { "value", "confidence" }
                                    }
                                },
                                publisher = new { type = "string" },
                                publish_year = new { type = "integer" },
                                language = new { type = "string" },
                                notes = new { type = "string" }
                            },
                            required = new[] { "title", "subtitle", "authors", "isbn_candidates", "publisher", "publish_year", "language", "notes" }
                        }
                    }
                }
            };
        }

        private static string ExtractJsonFromResponse(string body)
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("output", out var output))
            {
                return null;
            }

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content))
                {
                    continue;
                }

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var typeProp) &&
                        typeProp.GetString() == "output_text" &&
                        part.TryGetProperty("text", out var textProp))
                    {
                        return textProp.GetString();
                    }
                }
            }

            return null;
        }

        private static string ExtractTextFromResponse(string body)
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("output", out var output))
            {
                return null;
            }

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content))
                {
                    continue;
                }

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var typeProp) &&
                        typeProp.GetString() == "output_text" &&
                        part.TryGetProperty("text", out var textProp))
                    {
                        return textProp.GetString();
                    }
                }
            }

            return null;
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= max ? value : value.Substring(0, max) + "...";
        }

        private static object BuildOcrRequest(string model, List<string> imagePaths)
        {
            var contentParts = new List<object>
            {
                new
                {
                    type = "input_text",
                    text = "Extract all visible text from these images. Return plain text only."
                }
            };

            foreach (var path in imagePaths)
            {
                var bytes = File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:image/jpeg;base64,{base64}";
                contentParts.Add(new
                {
                    type = "input_image",
                    image_url = dataUrl
                });
            }

            return new
            {
                model,
                input = new[]
                {
                    new
                    {
                        role = "user",
                        content = contentParts
                    }
                }
            };
        }
    }

    public class BookExtractionResult
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<string> Authors { get; set; } = new List<string>();
        public List<IsbnCandidate> IsbnCandidates { get; set; } = new List<IsbnCandidate>();
        public string Publisher { get; set; }
        public int? PublishYear { get; set; }
        public string Language { get; set; }
        public string Notes { get; set; }
        public string RawJson { get; set; }
    }

    public class IsbnCandidate
    {
        public string Value { get; set; }
        public double Confidence { get; set; }
    }
}
