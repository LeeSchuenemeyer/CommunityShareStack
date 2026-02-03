using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Items
{
    [Authorize(Roles = "Admin,Librarian")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Item Item { get; set; }

        public SelectList ConditionOptions { get; set; }
        public SelectList ItemTypeOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (Item == null)
            {
                return NotFound();
            }

            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ConditionOptions = new SelectList(new[] { ItemCondition.New, ItemCondition.LikeNew, ItemCondition.Good, ItemCondition.Fair, ItemCondition.Poor });
            ItemTypeOptions = new SelectList(new[] { ItemType.Book, ItemType.Other });

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = Item.Id });
        }
    }
}
