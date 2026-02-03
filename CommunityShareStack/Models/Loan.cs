using System;

namespace CommunityShareStack.Models
{
    public class Loan
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }
        public DateTimeOffset CheckedOutAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset DueAt { get; set; }
        public DateTimeOffset? ReturnedAt { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.CheckedOut;
        public int RenewalCount { get; set; }
        public int MaxRenewals { get; set; }
        public int LateFeePerDayCents { get; set; }
    }
}
