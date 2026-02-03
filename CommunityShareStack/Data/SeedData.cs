using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityShareStack.Data
{
    public static class SeedData
    {
        private static readonly string[] Roles = new[] { "Admin", "Librarian", "Member" };

        public static async Task InitializeAsync(IServiceProvider services, string adminEmail)
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<ApplicationDbContext>();
            var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.MigrateAsync();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var adminUser = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        AutoApproveEligible = true,
                        FullName = "Admin"
                    };
                    var result = await userManager.CreateAsync(adminUser, "ChangeMe!123");
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException("Failed to create admin user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }

                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
