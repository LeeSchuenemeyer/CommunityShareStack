using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityShareStack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunityShareStack.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public List<UserRow> Users { get; set; } = new List<UserRow>();

        public SelectList RoleOptions { get; set; }

        public async Task OnGetAsync()
        {
            RoleOptions = new SelectList(new[] { "Member", "Librarian", "Admin" });
            var dbUsers = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            foreach (var dbUser in dbUsers)
            {
                var roles = await _userManager.GetRolesAsync(dbUser);
                Users.Add(new UserRow
                {
                    Id = dbUser.Id,
                    Email = dbUser.Email,
                    FullName = dbUser.FullName,
                    AutoApproveEligible = dbUser.AutoApproveEligible,
                    Role = roles.FirstOrDefault() ?? "Member"
                });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            RoleOptions = new SelectList(new[] { "Member", "Librarian", "Admin" });

            foreach (var userRow in Users)
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userRow.Id);
                if (user == null)
                {
                    continue;
                }

                user.FullName = userRow.FullName;
                user.AutoApproveEligible = userRow.AutoApproveEligible;
                await _userManager.UpdateAsync(user);

                var currentRoles = await _userManager.GetRolesAsync(user);
                foreach (var role in currentRoles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }

                if (!string.IsNullOrWhiteSpace(userRow.Role) && await _roleManager.RoleExistsAsync(userRow.Role))
                {
                    await _userManager.AddToRoleAsync(user, userRow.Role);
                }
            }

            return RedirectToPage();
        }

        public class UserRow
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string FullName { get; set; }
            public bool AutoApproveEligible { get; set; }
            public string Role { get; set; }
        }
    }
}
