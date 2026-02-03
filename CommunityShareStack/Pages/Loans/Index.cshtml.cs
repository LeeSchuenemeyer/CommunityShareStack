using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Loans
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Loan> ActiveLoans { get; set; } = new List<Loan>();
        public IList<LoanRequest> Requests { get; set; } = new List<LoanRequest>();
        public IList<HoldRequest> Holds { get; set; } = new List<HoldRequest>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            var activeLoans = await _context.Loans
                .Include(l => l.Item)
                .Where(l => l.UserId == user.Id && l.Status != LoanStatus.Returned)
                .AsNoTracking()
                .ToListAsync();
            ActiveLoans = activeLoans
                .OrderBy(l => l.DueAt)
                .ToList();

            var requests = await _context.LoanRequests
                .Include(r => r.Item)
                .Where(r => r.UserId == user.Id)
                .AsNoTracking()
                .ToListAsync();
            Requests = requests
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            Holds = await _context.HoldRequests
                .Include(h => h.Item)
                .Where(h => h.UserId == user.Id && h.IsActive)
                .OrderBy(h => h.Position)
                .ToListAsync();
        }
    }
}
