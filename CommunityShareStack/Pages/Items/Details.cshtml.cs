using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Items
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Item Item { get; set; }
        public IList<Review> Reviews { get; set; } = new List<Review>();
        public bool CanReview { get; set; }

        [BindProperty]
        public int Rating { get; set; }

        [BindProperty]
        public string Comment { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Item = await _context.Items
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (Item == null)
            {
                return NotFound();
            }

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ItemId == id)
                .AsNoTracking()
                .ToListAsync();
            Reviews = reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    CanReview = await _context.Loans.AnyAsync(l =>
                        l.ItemId == id && l.UserId == user.Id && l.Status == LoanStatus.CheckedOut);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (item.IsAvailable)
            {
                var loanRequest = new LoanRequest
                {
                    ItemId = item.Id,
                    UserId = user.Id,
                    Status = LoanRequestStatus.Requested
                };

                if (user.AutoApproveEligible && item.AutoApproveAllowed)
                {
                    loanRequest.Status = LoanRequestStatus.Approved;
                    loanRequest.ApprovedAt = System.DateTimeOffset.UtcNow;

                    var loan = new Loan
                    {
                        ItemId = item.Id,
                        UserId = user.Id,
                        DueAt = System.DateTimeOffset.UtcNow.AddDays(item.LoanDurationDays),
                        MaxRenewals = item.MaxRenewals,
                        LateFeePerDayCents = item.LateFeePerDayCents
                    };

                    item.IsAvailable = false;
                    _context.Loans.Add(loan);
                }

                _context.LoanRequests.Add(loanRequest);
                await _context.SaveChangesAsync();
                StatusMessage = loanRequest.Status == LoanRequestStatus.Approved
                    ? "Auto-approved. You can see your loan on the My Loans page."
                    : "Loan request submitted for approval.";
            }
            else
            {
                var position = await _context.HoldRequests.CountAsync(hr => hr.ItemId == item.Id && hr.IsActive) + 1;
                var hold = new HoldRequest
                {
                    ItemId = item.Id,
                    UserId = user.Id,
                    Position = position
                };
                _context.HoldRequests.Add(hold);
                await _context.SaveChangesAsync();
                StatusMessage = $"Added to the waitlist. Your position is {position}.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostReviewAsync(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var canReview = await _context.Loans.AnyAsync(l =>
                l.ItemId == id && l.UserId == user.Id && l.Status == LoanStatus.CheckedOut);
            if (!canReview)
            {
                StatusMessage = "You can only review items you currently have checked out.";
                return RedirectToPage(new { id });
            }

            if (Rating < 0 || Rating > 4)
            {
                StatusMessage = "Rating must be between 0 and 4 stars.";
                return RedirectToPage(new { id });
            }

            var review = new Review
            {
                ItemId = id,
                UserId = user.Id,
                Rating = Rating,
                Comment = Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            StatusMessage = "Review submitted.";
            return RedirectToPage(new { id });
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

            if (string.IsNullOrWhiteSpace(featuredUrl) || !item.Images.Any(i => i.ImageUrl == featuredUrl))
            {
                StatusMessage = "Select an image to feature.";
                return RedirectToPage(new { id });
            }

            item.FeaturedImageUrl = featuredUrl;
            await _context.SaveChangesAsync();
            StatusMessage = "Featured image updated.";
            return RedirectToPage(new { id });
        }
    }
}
