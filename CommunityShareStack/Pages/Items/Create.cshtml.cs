using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CommunityShareStack.Services;

namespace CommunityShareStack.Pages.Items
{
    [Authorize(Roles = "Admin,Librarian")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly OpenLibraryClient _openLibraryClient;

        public CreateModel(ApplicationDbContext context, OpenLibraryClient openLibraryClient)
        {
            _context = context;
            _openLibraryClient = openLibraryClient;
        }

        [BindProperty]
        public Item Item { get; set; } = new Item();

        [BindProperty]
        public string BookTitleQuery { get; set; }

        [BindProperty]
        public OpenLibrarySearchMode BookSearchMode { get; set; } = OpenLibrarySearchMode.Title;

        [BindProperty]
        public List<BookSearchResult> SearchResults { get; set; } = new List<BookSearchResult>();

        [BindProperty]
        public int? SelectedIndex { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public SelectList ConditionOptions { get; set; }
        public SelectList ItemTypeOptions { get; set; }

        public void OnGet()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}".Trim()))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
                StatusMessage = errors.Count > 0 ? $"Save failed: {string.Join("; ", errors)}" : "Save failed due to invalid input.";
                return Page();
            }

            Item.IsActive = true;
            Item.IsAvailable = true;

            try
            {
                _context.Items.Add(Item);
                await _context.SaveChangesAsync();
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
                return Page();
            }

            return RedirectToPage("./Details", new { id = Item.Id });
        }

        public async Task<IActionResult> OnPostLookupAsync()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });

            if (string.IsNullOrWhiteSpace(BookTitleQuery))
            {
                StatusMessage = "Enter a book title to search.";
                return Page();
            }

            var results = await _openLibraryClient.SearchAsync(BookTitleQuery, BookSearchMode, 8);
            if (results == null || results.Count == 0)
            {
                StatusMessage = "No results found for that search.";
                return Page();
            }

            SearchResults = results
                .Select(r => new BookSearchResult
                {
                    Title = r.Title,
                    Isbn = r.Isbn,
                    WorkKey = r.WorkKey,
                    EditionKey = r.EditionKey,
                    AuthorName = r.AuthorName,
                    FirstPublishYear = r.FirstPublishYear,
                    CoverId = r.CoverId,
                    CoverUrl = r.CoverUrl
                })
                .ToList();

            StatusMessage = "Select a match to populate the form.";
            ModelState.Clear();
            return Page();
        }

        public async Task<IActionResult> OnPostSelectAsync()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });

            if (!SelectedIndex.HasValue || SelectedIndex.Value < 0 || SelectedIndex.Value >= SearchResults.Count)
            {
                StatusMessage = "Select a result to continue.";
                return Page();
            }

            var selected = SearchResults[SelectedIndex.Value];
            var details = !string.IsNullOrWhiteSpace(selected.Isbn)
                ? await _openLibraryClient.LookupByIsbnAsync(selected.Isbn)
                : await _openLibraryClient.LookupByEditionKeyAsync(selected.EditionKey);

            Item.Title = selected.Title;
            Item.ItemType = ItemType.Book;
            Item.Isbn = selected.Isbn;
            Item.BookAuthor = selected.AuthorName;
            Item.FeaturedImageUrl = selected.CoverUrl;
            Item.OpenLibraryWorkKey = selected.WorkKey;
            Item.OpenLibraryEditionKey = selected.EditionKey;
            Item.OpenLibraryJson = details?.OpenLibraryJson;

            if (string.IsNullOrWhiteSpace(Item.Category))
            {
                Item.Category = "Book";
            }

            if (Item.Condition == ItemCondition.New)
            {
                Item.Condition = ItemCondition.Good;
            }

            StatusMessage = "Book data pulled from OpenLibrary. Review and save.";
            ModelState.Clear();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAndNewAsync()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value.Errors.Select(err => $"{kvp.Key}: {err.ErrorMessage}".Trim()))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
                StatusMessage = errors.Count > 0 ? $"Save failed: {string.Join("; ", errors)}" : "Save failed due to invalid input.";
                return Page();
            }

            Item.IsActive = true;
            Item.IsAvailable = true;

            try
            {
                _context.Items.Add(Item);
                await _context.SaveChangesAsync();
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
                return Page();
            }

            StatusMessage = "Saved. You can add another item.";
            ModelState.Clear();
            Item = new Item();
            return Page();
        }

        public class BookSearchResult
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
    }
}
