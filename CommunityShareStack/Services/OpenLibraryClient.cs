using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CommunityShareStack.Services
{
    public class OpenLibraryClient
    {
        private readonly HttpClient _httpClient;

        public OpenLibraryClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://openlibrary.org/");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CommunityShareStack/1.0 (contact: lschuenemeyer@gmail.com)");
        }

        public async Task<List<OpenLibrarySearchResult>> SearchAsync(string query, OpenLibrarySearchMode mode, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var searchUrl = BuildSearchUrl(query, mode, limit);
            var searchResponse = await _httpClient.GetAsync(searchUrl);
            if (!searchResponse.IsSuccessStatusCode)
            {
                return null;
            }

            await using var searchStream = await searchResponse.Content.ReadAsStreamAsync();
            var search = await JsonSerializer.DeserializeAsync<OpenLibrarySearchResponse>(searchStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (search == null || search.Docs == null || search.Docs.Count == 0)
            {
                return null;
            }

            var results = new List<OpenLibrarySearchResult>();
            foreach (var doc in search.Docs)
            {
                results.Add(new OpenLibrarySearchResult
                {
                    Title = doc.Title,
                    Isbn = PickPreferredIsbn(doc.Isbn),
                    WorkKey = doc.Key,
                    EditionKey = doc.EditionKey != null && doc.EditionKey.Count > 0 ? doc.EditionKey[0] : null,
                    AuthorName = doc.AuthorName != null && doc.AuthorName.Count > 0 ? string.Join(", ", doc.AuthorName) : null,
                    FirstPublishYear = doc.FirstPublishYear,
                    CoverId = doc.CoverI,
                    CoverUrl = doc.CoverI.HasValue ? $"https://covers.openlibrary.org/b/id/{doc.CoverI.Value}-M.jpg" : null
                });
            }

            return results;
        }

        private static string BuildSearchUrl(string query, OpenLibrarySearchMode mode, int limit)
        {
            var encoded = Uri.EscapeDataString(query);
            var fields = "key,title,edition_key,isbn,author_name,first_publish_year,cover_i";

            return mode switch
            {
                OpenLibrarySearchMode.Title => $"search.json?title={encoded}&limit={limit}&fields={fields}",
                OpenLibrarySearchMode.Author => $"search.json?author={encoded}&limit={limit}&fields={fields}",
                OpenLibrarySearchMode.Isbn => $"search.json?isbn={encoded}&limit={limit}&fields={fields}",
                _ => $"search.json?q={encoded}&limit={limit}&fields={fields}"
            };
        }

        public async Task<OpenLibraryLookupResult> LookupByIsbnAsync(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return null;
            }

            var bookUrl = $"api/books?bibkeys=ISBN:{Uri.EscapeDataString(isbn)}&format=json&jscmd=data";
            var bookResponse = await _httpClient.GetAsync(bookUrl);
            if (!bookResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var bookJson = await bookResponse.Content.ReadAsStringAsync();
            return new OpenLibraryLookupResult
            {
                Isbn = isbn,
                OpenLibraryJson = bookJson
            };
        }

        public async Task<OpenLibraryLookupResult> LookupByEditionKeyAsync(string editionKey)
        {
            if (string.IsNullOrWhiteSpace(editionKey))
            {
                return null;
            }

            var bookUrl = $"api/books?bibkeys=OLID:{Uri.EscapeDataString(editionKey)}&format=json&jscmd=data";
            var bookResponse = await _httpClient.GetAsync(bookUrl);
            if (!bookResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var bookJson = await bookResponse.Content.ReadAsStringAsync();
            return new OpenLibraryLookupResult
            {
                EditionKey = editionKey,
                OpenLibraryJson = bookJson
            };
        }

        private static string PickPreferredIsbn(List<string> isbns)
        {
            if (isbns == null || isbns.Count == 0)
            {
                return null;
            }

            foreach (var isbn in isbns)
            {
                if (!string.IsNullOrWhiteSpace(isbn) && isbn.Length == 13)
                {
                    return isbn;
                }
            }

            foreach (var isbn in isbns)
            {
                if (!string.IsNullOrWhiteSpace(isbn))
                {
                    return isbn;
                }
            }

            return null;
        }

        private class OpenLibrarySearchResponse
        {
            [JsonPropertyName("docs")]
            public List<OpenLibrarySearchDoc> Docs { get; set; }
        }

        private class OpenLibrarySearchDoc
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }
            [JsonPropertyName("title")]
            public string Title { get; set; }
            [JsonPropertyName("edition_key")]
            public List<string> EditionKey { get; set; }
            [JsonPropertyName("isbn")]
            public List<string> Isbn { get; set; }
            [JsonPropertyName("author_name")]
            public List<string> AuthorName { get; set; }
            [JsonPropertyName("first_publish_year")]
            public int? FirstPublishYear { get; set; }
            [JsonPropertyName("cover_i")]
            public int? CoverI { get; set; }
        }
    }

    public class OpenLibraryLookupResult
    {
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string WorkKey { get; set; }
        public string EditionKey { get; set; }
        public string OpenLibraryJson { get; set; }
    }

    public class OpenLibrarySearchResult
    {
        public string Title { get; set; }
        public string Isbn { get; set; }
        public string WorkKey { get; set; }
        public string EditionKey { get; set; }
        public string AuthorName { get; set; }
        public int? FirstPublishYear { get; set; }
        public int? CoverId { get; set; }
        public string CoverUrl { get; set; }
    }

    public enum OpenLibrarySearchMode
    {
        Title = 0,
        Author = 1,
        Isbn = 2,
        All = 3
    }
}
