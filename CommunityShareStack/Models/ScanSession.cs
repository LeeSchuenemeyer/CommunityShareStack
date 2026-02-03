using System;
using System.Collections.Generic;

namespace CommunityShareStack.Models
{
    public class ScanSession
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Data.ApplicationUser User { get; set; }
        public ScanStatus Status { get; set; } = ScanStatus.Uploaded;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Authors { get; set; }
        public string Isbn { get; set; }
        public double? IsbnConfidence { get; set; }
        public string Publisher { get; set; }
        public int? PublishYear { get; set; }
        public string Language { get; set; }
        public string Notes { get; set; }
        public string OcrText { get; set; }
        public string RawJson { get; set; }
        public string ErrorMessage { get; set; }
        public List<ScanImage> Images { get; set; } = new List<ScanImage>();
    }

    public class ScanImage
    {
        public int Id { get; set; }
        public int ScanSessionId { get; set; }
        public ScanSession ScanSession { get; set; }
        public string ImageUrl { get; set; }
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public enum ScanStatus
    {
        Uploaded = 0,
        Analyzing = 1,
        Analyzed = 2,
        Completed = 3,
        Failed = 4
    }
}
