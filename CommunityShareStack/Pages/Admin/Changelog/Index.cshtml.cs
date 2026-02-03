using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CommunityShareStack.Pages.Admin.Changelog
{
    [Authorize(Roles = "Admin,Librarian")]
    public class IndexModel : PageModel
    {
        public List<ChangelogEntry> Entries { get; set; } = new List<ChangelogEntry>
        {
            
            
            
            
            new ChangelogEntry
            {
                Version = "0.3.6",
                Date = new DateTime(2026, 2, 1),
                Notes = new List<string>
                {
                    "TODO: add release notes"
                }
            },
new ChangelogEntry
            {
                Version = "0.3.5",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "TODO: add release notes"
                }
            },
new ChangelogEntry
            {
                Version = "0.3.4",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "TODO: add release notes"
                }
            },
new ChangelogEntry
            {
                Version = "0.3.3",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "TODO: add release notes"
                }
            },
new ChangelogEntry
            {
                Version = "0.3.2",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "Fixed OpenAI structured output schema to include subtitle.",
                    "Improved scan analysis error clarity."
                }
            },
            new ChangelogEntry
            {
                Version = "0.3.1",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "Added scan sessions with ChatGPT Vision structured extraction and OCR fallback.",
                    "Added editable extraction preview before creating items.",
                    "Added background analysis queue and status polling.",
                    "Added OpenAI diagnostics page."
                }
            },
            new ChangelogEntry
            {
                Version = "0.3.0",
                Date = new DateTime(2026, 1, 31),
                Notes = new List<string>
                {
                    "Added admin dashboard with approvals and reminders.",
                    "Added reviews and reviews index.",
                    "Added edit/delete for items and waitlist management.",
                    "Added import/export and loan history pages.",
                    "Updated modern library UI and item card layout."
                }
            }
        };

        public void OnGet()
        {
        }
    }

    public class ChangelogEntry
    {
        public string Version { get; set; }
        public DateTime Date { get; set; }
        public List<string> Notes { get; set; } = new List<string>();
    }
}




