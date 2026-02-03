using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Item> AvailableItems { get; set; } = new List<Item>();

        public async Task OnGetAsync()
        {
            AvailableItems = await _context.Items
                .Where(i => i.IsActive && i.IsAvailable)
                .OrderBy(i => i.Title)
                .Take(12)
                .ToListAsync();
        }
    }
}
