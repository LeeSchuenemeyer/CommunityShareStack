using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CommunityShareStack.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        public ItemCondition Condition { get; set; }

        [Range(0, 1000000)]
        public decimal? EstimatedValue { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        [MaxLength(100)]
        public string UniqueId { get; set; }

        public ItemType ItemType { get; set; }

        public bool IsActive { get; set; } = true;

        public bool AutoApproveAllowed { get; set; } = true;

        public int LoanDurationDays { get; set; } = 14;

        public int MaxRenewals { get; set; } = 1;

        public int LateFeePerDayCents { get; set; } = 0;

        public bool IsAvailable { get; set; } = true;

        public string Isbn { get; set; }

        public string BookAuthor { get; set; }

        public string FeaturedImageUrl { get; set; }

        public string OpenLibraryWorkKey { get; set; }

        public string OpenLibraryEditionKey { get; set; }

        public string OpenLibraryJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<ItemImage> Images { get; set; } = new List<ItemImage>();
    }
}
