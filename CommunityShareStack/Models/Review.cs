using System;
using System.ComponentModel.DataAnnotations;

namespace CommunityShareStack.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }

        [Range(0, 4)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string Comment { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
