using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Admin.Waitlist
{
    [Authorize(Roles = "Admin,Librarian")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public IndexModel(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public List<WaitlistGroup> Waitlists { get; set; } = new List<WaitlistGroup>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var holds = await _context.HoldRequests
                .Include(h => h.Item)
                .Include(h => h.User)
                .Where(h => h.IsActive)
                .OrderBy(h => h.ItemId)
                .ThenBy(h => h.Position)
                .AsNoTracking()
                .ToListAsync();

            Waitlists = holds
                .GroupBy(h => h.ItemId)
                .Select(g => new WaitlistGroup
                {
                    ItemId = g.Key,
                    ItemTitle = g.First().Item?.Title,
                    Holds = g.Select(h => new HoldRow
                    {
                        Id = h.Id,
                        Position = h.Position,
                        UserEmail = h.User?.Email,
                        RequestedAt = h.RequestedAt
                    }).ToList()
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostMoveAsync(int holdId, string direction)
        {
            var hold = await _context.HoldRequests.FirstOrDefaultAsync(h => h.Id == holdId && h.IsActive);
            if (hold == null)
            {
                return RedirectToPage();
            }

            var swapPosition = direction == "up" ? hold.Position - 1 : hold.Position + 1;
            if (swapPosition < 1)
            {
                return RedirectToPage();
            }

            var swapHold = await _context.HoldRequests
                .FirstOrDefaultAsync(h => h.ItemId == hold.ItemId && h.Position == swapPosition && h.IsActive);
            if (swapHold == null)
            {
                return RedirectToPage();
            }

            hold.Position = swapPosition;
            swapHold.Position = direction == "up" ? swapPosition + 1 : swapPosition - 1;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostNotifyNextAsync(int itemId)
        {
            var nextHold = await _context.HoldRequests
                .Include(h => h.User)
                .Include(h => h.Item)
                .Where(h => h.ItemId == itemId && h.IsActive)
                .OrderBy(h => h.Position)
                .FirstOrDefaultAsync();

            if (nextHold?.User != null)
            {
                await _emailSender.SendAsync(nextHold.User.Email, "Waitlist Available", $"You are next for '{nextHold.Item?.Title}'. Please request the item.");
                StatusMessage = "Notification sent to next borrower.";
            }
            else
            {
                StatusMessage = "No active waitlist entries.";
            }

            return RedirectToPage();
        }

        public class WaitlistGroup
        {
            public int ItemId { get; set; }
            public string ItemTitle { get; set; }
            public List<HoldRow> Holds { get; set; } = new List<HoldRow>();
        }

        public class HoldRow
        {
            public int Id { get; set; }
            public int Position { get; set; }
            public string UserEmail { get; set; }
            public System.DateTimeOffset RequestedAt { get; set; }
        }
    }
}
