using System;

namespace CommunityShareStack.Models
{
    public class LoanRequest
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public LoanRequestStatus Status { get; set; } = LoanRequestStatus.Requested;
        public DateTimeOffset? ApprovedAt { get; set; }
        public string DecisionNotes { get; set; }
    }
}
