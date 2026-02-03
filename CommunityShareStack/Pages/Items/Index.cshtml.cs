using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Items
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Item> Items { get; set; } = new List<Item>();
        public IDictionary<int, double> AverageRatings { get; set; } = new Dictionary<int, double>();

        [BindProperty(SupportsGet = true)]
        public string SearchText { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SearchTitle { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SearchAuthor { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SearchIsbn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Availability { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public ItemCondition? Condition { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Rating { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Items
                .Include(i => i.Images)
                .Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(Availability))
            {
                if (Availability == "available")
                {
                    query = query.Where(i => i.IsAvailable);
                }
                else if (Availability == "checkedout")
                {
                    query = query.Where(i => !i.IsAvailable);
                }
            }

            if (!string.IsNullOrWhiteSpace(Category))
            {
                query = query.Where(i => i.Category != null && i.Category.Contains(Category));
            }

            if (Condition.HasValue)
            {
                query = query.Where(i => i.Condition == Condition.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var useTitle = SearchTitle || (!SearchTitle && !SearchAuthor && !SearchIsbn);
                var useAuthor = SearchAuthor || (!SearchTitle && !SearchAuthor && !SearchIsbn);
                var useIsbn = SearchIsbn || (!SearchTitle && !SearchAuthor && !SearchIsbn);

                query = query.Where(i =>
                    (useTitle && i.Title.Contains(SearchText)) ||
                    (useAuthor && i.BookAuthor != null && i.BookAuthor.Contains(SearchText)) ||
                    (useIsbn && i.Isbn != null && i.Isbn.Contains(SearchText)));
            }

            Items = await query
                .OrderBy(i => i.Title)
                .ToListAsync();

            var itemIds = Items.Select(i => i.Id).ToList();
            var ratings = await _context.Reviews
                .Where(r => itemIds.Contains(r.ItemId))
                .AsNoTracking()
                .ToListAsync();
            AverageRatings = ratings
                .GroupBy(r => r.ItemId)
                .ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

            if (Rating.HasValue)
            {
                Items = Items
                    .Where(i => AverageRatings.ContainsKey(i.Id) && AverageRatings[i.Id] >= Rating.Value)
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostSetFeaturedAsync(int id, string featuredUrl)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Librarian"))
            {
                return Forbid();
            }

            var item = await _context.Items
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(featuredUrl))
            {
                return RedirectToPage();
            }

            if (!item.Images.Any(i => i.ImageUrl == featuredUrl) && item.FeaturedImageUrl != featuredUrl)
            {
                return RedirectToPage();
            }

            item.FeaturedImageUrl = featuredUrl;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
