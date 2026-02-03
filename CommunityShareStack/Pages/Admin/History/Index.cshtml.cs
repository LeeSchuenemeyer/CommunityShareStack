using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Admin.History
{
    [Authorize(Roles = "Admin,Librarian")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string UserEmail { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ItemId { get; set; }

        public IList<Loan> Loans { get; set; } = new List<Loan>();

        public async Task OnGetAsync()
        {
            var query = _context.Loans
                .Include(l => l.Item)
                .Include(l => l.User)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(UserEmail))
            {
                query = query.Where(l => l.User.Email.Contains(UserEmail));
            }

            if (ItemId.HasValue)
            {
                query = query.Where(l => l.ItemId == ItemId.Value);
            }

            var loansList = await query.ToListAsync();
            Loans = loansList
                .OrderByDescending(l => l.CheckedOutAt)
                .ToList();
        }
    }
}
