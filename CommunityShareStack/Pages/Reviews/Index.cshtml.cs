using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Reviews
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Rating { get; set; }

        public IList<ItemWithReviews> Items { get; set; } = new List<ItemWithReviews>();

        public async Task OnGetAsync()
        {
            var reviewsQuery = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Item)
                .Where(r => r.Item.IsActive);

            if (!string.IsNullOrWhiteSpace(Category))
            {
                reviewsQuery = reviewsQuery.Where(r => r.Item.Category.Contains(Category));
            }

            if (Rating.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == Rating.Value);
            }

            var reviewsList = await reviewsQuery
                .AsNoTracking()
                .ToListAsync();
            var reviews = reviewsList
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            Items = reviews
                .GroupBy(r => r.ItemId)
                .Select(g => new ItemWithReviews
                {
                    Id = g.First().ItemId,
                    Title = g.First().Item.Title,
                    Category = g.First().Item.Category,
                    Reviews = g.ToList()
                })
                .ToList();
        }

        public class ItemWithReviews
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Category { get; set; }
            public List<Review> Reviews { get; set; } = new List<Review>();
        }
    }
}
