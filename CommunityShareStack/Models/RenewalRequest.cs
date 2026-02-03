using System;

namespace CommunityShareStack.Models
{
    public class RenewalRequest
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool Approved { get; set; }
        public DateTimeOffset? DecisionAt { get; set; }
        public string DecisionNotes { get; set; }
    }
}
