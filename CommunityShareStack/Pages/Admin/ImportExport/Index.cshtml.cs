using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Admin.ImportExport
{
    [Authorize(Roles = "Admin,Librarian")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnPostExportAsync()
        {
            var items = await _context.Items
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Title)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Title,Description,Category,Condition,ItemType,Isbn,BookAuthor,EstimatedValue,UniqueId,Notes");
            foreach (var item in items)
            {
                sb.AppendLine(string.Join(",",
                    Escape(item.Title),
                    Escape(item.Description),
                    Escape(item.Category),
                    Escape(item.Condition.ToString()),
                    Escape(item.ItemType.ToString()),
                    Escape(item.Isbn),
                    Escape(item.BookAuthor),
                    Escape(item.EstimatedValue?.ToString(CultureInfo.InvariantCulture)),
                    Escape(item.UniqueId),
                    Escape(item.Notes)));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "inventory.csv");
        }

        public async Task<IActionResult> OnPostImportAsync()
        {
            var file = Request.Form.Files["Upload"];
            if (file == null || file.Length == 0)
            {
                StatusMessage = "Please upload a CSV file.";
                return Page();
            }

            var items = new List<Item>();
            using var reader = new StreamReader(file.OpenReadStream());
            var header = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(header))
            {
                StatusMessage = "CSV is empty.";
                return Page();
            }

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = SplitCsv(line);
                if (parts.Count < 10)
                {
                    continue;
                }

                var item = new Item
                {
                    Title = parts[0],
                    Description = parts[1],
                    Category = parts[2],
                    Condition = Enum.TryParse(parts[3], out ItemCondition condition) ? condition : ItemCondition.Good,
                    ItemType = Enum.TryParse(parts[4], out ItemType type) ? type : ItemType.Other,
                    Isbn = parts[5],
                    BookAuthor = parts[6],
                    EstimatedValue = decimal.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : (decimal?)null,
                    UniqueId = parts[8],
                    Notes = parts[9],
                    IsActive = true,
                    IsAvailable = true
                };
                items.Add(item);
            }

            if (items.Count == 0)
            {
                StatusMessage = "No items imported.";
                return Page();
            }

            _context.Items.AddRange(items);
            await _context.SaveChangesAsync();
            StatusMessage = $"Imported {items.Count} item(s).";
            return Page();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var needsQuotes = value.Contains(",") || value.Contains("\"") || value.Contains("\n");
            value = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{value}\"" : value;
        }

        private static List<string> SplitCsv(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"' )
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}
