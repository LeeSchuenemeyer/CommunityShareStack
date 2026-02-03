using System.Threading.Tasks;
using CommunityShareStack.Data;
using CommunityShareStack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Items
{
    [Authorize(Roles = "Admin,Librarian")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Item Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
            if (Item == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == Item.Id);
            if (item == null)
            {
                return NotFound();
            }

            item.IsActive = false;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
