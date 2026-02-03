using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using CommunityShareStack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Admin.Dashboard
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

        public int ActiveLoansCount { get; set; }
        public int OverdueCount { get; set; }
        public int PendingRequestsCount { get; set; }

        public IList<LoanRequest> PendingRequests { get; set; } = new List<LoanRequest>();
        public IList<Loan> OverdueLoans { get; set; } = new List<Loan>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var loans = await _context.Loans
                .AsNoTracking()
                .ToListAsync();
            ActiveLoansCount = loans.Count(l => l.Status != LoanStatus.Returned);
            OverdueCount = loans.Count(l => l.Status != LoanStatus.Returned && l.DueAt < now);

            var requests = await _context.LoanRequests
                .AsNoTracking()
                .ToListAsync();
            PendingRequestsCount = requests.Count(r => r.Status == LoanRequestStatus.Requested);

            PendingRequests = await _context.LoanRequests
                .Include(r => r.Item)
                .Include(r => r.User)
                .Where(r => r.Status == LoanRequestStatus.Requested)
                .AsNoTracking()
                .ToListAsync();

            var overdueList = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.User)
                .AsNoTracking()
                .ToListAsync();
            OverdueLoans = overdueList
                .Where(l => l.Status != LoanStatus.Returned && l.DueAt < now)
                .OrderBy(l => l.DueAt)
                .ToList();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var request = await _context.LoanRequests
                .Include(r => r.Item)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null || request.Status != LoanRequestStatus.Requested)
            {
                return RedirectToPage();
            }

            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
            if (item == null || !item.IsAvailable)
            {
                StatusMessage = "Item is not available.";
                return RedirectToPage();
            }

            request.Status = LoanRequestStatus.Approved;
            request.ApprovedAt = DateTimeOffset.UtcNow;

            var loan = new Loan
            {
                ItemId = item.Id,
                UserId = request.UserId,
                DueAt = DateTimeOffset.UtcNow.AddDays(item.LoanDurationDays),
                MaxRenewals = item.MaxRenewals,
                LateFeePerDayCents = item.LateFeePerDayCents
            };

            item.IsAvailable = false;
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            if (request.User != null)
            {
                await _emailSender.SendAsync(request.User.Email, "Loan Approved", $"Your request for '{item.Title}' has been approved.");
            }

            StatusMessage = "Request approved.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDenyAsync(int id)
        {
            var request = await _context.LoanRequests
                .Include(r => r.Item)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null || request.Status != LoanRequestStatus.Requested)
            {
                return RedirectToPage();
            }

            request.Status = LoanRequestStatus.Rejected;
            await _context.SaveChangesAsync();

            if (request.User != null)
            {
                await _emailSender.SendAsync(request.User.Email, "Loan Denied", $"Your request for '{request.Item?.Title}' was denied.");
            }

            StatusMessage = "Request denied.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendRemindersAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var dueSoon = now.AddDays(2);
            var loans = await _context.Loans
                .Include(l => l.Item)
                .Include(l => l.User)
                .Where(l => l.Status != LoanStatus.Returned && l.DueAt <= dueSoon)
                .AsNoTracking()
                .ToListAsync();

            foreach (var loan in loans)
            {
                if (loan.User != null)
                {
                    await _emailSender.SendAsync(loan.User.Email, "Loan Reminder", $"Your loan for '{loan.Item?.Title}' is due on {loan.DueAt.LocalDateTime:d}.");
                }
            }

            StatusMessage = $"Sent {loans.Count} reminder(s).";
            return RedirectToPage();
        }
    }
}
