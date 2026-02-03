using System;

namespace CommunityShareStack.Models
{
    public class HoldRequest
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsActive { get; set; } = true;
        public int Position { get; set; }
    }
}
