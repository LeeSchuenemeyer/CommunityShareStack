using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using CommunityShareStack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityShareStack.Pages.Scan
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OpenAiVisionClient _visionClient;
        private readonly OpenLibraryClient _openLibraryClient;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public IndexModel(IWebHostEnvironment env, ApplicationDbContext context, UserManager<ApplicationUser> userManager, OpenAiVisionClient visionClient, OpenLibraryClient openLibraryClient, IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory)
        {
            _env = env;
            _context = context;
            _userManager = userManager;
            _visionClient = visionClient;
            _openLibraryClient = openLibraryClient;
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
        }

        public IList<ScanSession> RecentSessions { get; set; } = new List<ScanSession>();
        public ScanSession Session { get; set; }
        public IList<IsbnCandidate> Candidates { get; set; } = new List<IsbnCandidate>();

        [BindProperty]
        public ConfirmInput Input { get; set; } = new ConfirmInput();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync(int? id = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            var recent = await _context.ScanSessions
                .Include(s => s.Images)
                .Where(s => s.UserId == user.Id)
                .AsNoTracking()
                .ToListAsync();
            RecentSessions = recent
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToList();

            if (id.HasValue)
            {
                Session = await _context.ScanSessions
                    .Include(s => s.Images)
                    .FirstOrDefaultAsync(s => s.Id == id.Value && s.UserId == user.Id);
                if (Session != null)
                {
                    Candidates = ParseCandidates(Session.RawJson);
                    Input.Title = Session.Title;
                    Input.Subtitle = Session.Subtitle;
                    Input.Authors = Session.Authors;
                    Input.Isbn = Session.Isbn;
                    Input.Publisher = Session.Publisher;
                    Input.PublishYear = Session.PublishYear;
                    Input.Language = Session.Language;
                    Input.Notes = Session.Notes;
                }
            }
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var files = Request.Form.Files.GetFiles("Uploads");
            if (files == null || files.Count == 0)
            {
                StatusMessage = "Please choose at least one image.";
                return RedirectToPage();
            }

            var session = new ScanSession
            {
                UserId = user.Id,
                Status = ScanStatus.Uploaded
            };
            _context.ScanSessions.Add(session);
            await _context.SaveChangesAsync();

            var uploadsPath = Path.Combine(_env.WebRootPath, "scans", session.Id.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var file in files)
            {
                var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, safeFileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.ScanImages.Add(new ScanImage
                {
                    ScanSessionId = session.Id,
                    ImageUrl = $"/scans/{session.Id}/{safeFileName}"
                });
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Scan session created.";
            return RedirectToPage(new { id = session.Id });
        }

        public async Task<IActionResult> OnPostAnalyzeAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var session = await _context.ScanSessions
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == user.Id);
            if (session == null)
            {
                return NotFound();
            }

            if (session.Status == ScanStatus.Analyzing)
            {
                return RedirectToPage(new { id = session.Id });
            }

            session.Status = ScanStatus.Analyzing;
            session.ErrorMessage = null;
            await _context.SaveChangesAsync();

            var sessionId = session.Id;
            _taskQueue.QueueBackgroundWorkItem(async _ =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var scopedEnv = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    var scopedVision = scope.ServiceProvider.GetRequiredService<OpenAiVisionClient>();

                    var scopedSession = await scopedContext.ScanSessions
                        .Include(s => s.Images)
                        .FirstOrDefaultAsync(s => s.Id == sessionId);
                    if (scopedSession == null)
                    {
                        return;
                    }

                    var imagePaths = scopedSession.Images
                        .Select(i => Path.Combine(scopedEnv.WebRootPath, i.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())))
                        .ToList();

                    var result = await scopedVision.ExtractBookAsync(imagePaths);
                    scopedSession.Title = result.Title;
                    scopedSession.Subtitle = result.Subtitle;
                    scopedSession.Authors = result.Authors != null ? string.Join(", ", result.Authors) : null;
                    var picked = PickBestIsbn(result.IsbnCandidates);
                    scopedSession.Isbn = picked?.Value;
                    scopedSession.IsbnConfidence = picked?.Confidence;
                    scopedSession.Publisher = result.Publisher;
                    scopedSession.PublishYear = result.PublishYear;
                    scopedSession.Language = result.Language;
                    scopedSession.Notes = result.Notes;
                    scopedSession.RawJson = result.RawJson;

                    if (string.IsNullOrWhiteSpace(scopedSession.Isbn))
                    {
                        scopedSession.OcrText = await scopedVision.ExtractOcrTextAsync(imagePaths);
                        scopedSession.Isbn = OpenAiVisionClient.TryFindIsbnFromText(scopedSession.OcrText);
                        scopedSession.IsbnConfidence = scopedSession.Isbn != null ? 0.5 : (double?)null;
                    }

                    scopedSession.Status = ScanStatus.Analyzed;
                    scopedSession.ErrorMessage = null;
                    await scopedContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var scopedSession = await scopedContext.ScanSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                    if (scopedSession != null)
                    {
                        scopedSession.Status = ScanStatus.Failed;
                        scopedSession.ErrorMessage = ex.Message;
                        await scopedContext.SaveChangesAsync();
                    }
                }
            });

            StatusMessage = "Analysis started. This may take a minute.";
            return RedirectToPage(new { id = session.Id });
        }

        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var session = await _context.ScanSessions
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == user.Id);
            if (session == null || session.Status != ScanStatus.Analyzed)
            {
                return RedirectToPage(new { id });
            }

            var item = new Item
            {
                Title = string.IsNullOrWhiteSpace(Input.Title) ? session.Title : Input.Title,
                BookAuthor = string.IsNullOrWhiteSpace(Input.Authors) ? session.Authors : Input.Authors,
                Isbn = string.IsNullOrWhiteSpace(Input.Isbn) ? session.Isbn : Input.Isbn,
                ItemType = ItemType.Book,
                Category = "Book",
                Condition = ItemCondition.Good,
                IsActive = true,
                IsAvailable = true,
                Notes = string.IsNullOrWhiteSpace(Input.Notes) ? session.Notes : Input.Notes
            };

            if (!string.IsNullOrWhiteSpace(item.Isbn))
            {
                var details = await _openLibraryClient.LookupByIsbnAsync(item.Isbn);
                item.OpenLibraryJson = details?.OpenLibraryJson;
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            foreach (var scanImage in session.Images)
            {
                _context.ItemImages.Add(new ItemImage
                {
                    ItemId = item.Id,
                    ImageUrl = scanImage.ImageUrl
                });
            }

            if (session.Images.Count > 0)
            {
                item.FeaturedImageUrl = session.Images[0].ImageUrl;
            }

            session.Status = ScanStatus.Completed;
            await _context.SaveChangesAsync();

            StatusMessage = "Item created from scan.";
            return RedirectToPage("/Items/Details", new { id = item.Id });
        }

        private static IsbnCandidate PickBestIsbn(List<IsbnCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var ordered = candidates
                .OrderByDescending(c => c.Confidence)
                .ToList();

            var isbn13 = ordered.FirstOrDefault(c => c.Value != null && c.Value.Length == 13);
            return isbn13 ?? ordered[0];
        }

        private static IList<IsbnCandidate> ParseCandidates(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new List<IsbnCandidate>();
            }

            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<BookExtractionResult>(rawJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result?.IsbnCandidates ?? new List<IsbnCandidate>();
            }
            catch
            {
                return new List<IsbnCandidate>();
            }
        }

        public class ConfirmInput
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string Authors { get; set; }
            public string Isbn { get; set; }
            public string Publisher { get; set; }
            public int? PublishYear { get; set; }
            public string Language { get; set; }
            public string Notes { get; set; }
        }
    }
}
