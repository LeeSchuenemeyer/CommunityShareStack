using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommunityShareStack.Data
{
    public static class SeedData
    {
        private static readonly string[] Roles = new[] { "Admin", "Librarian", "Member" };

        public static async Task InitializeAsync(IServiceProvider services, string adminEmail, string adminPassword)
        {
            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<ApplicationDbContext>();
            var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scopedServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedData));

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
                if (adminUser == null && string.IsNullOrWhiteSpace(adminPassword))
                {
                    // No password configured: don't create an account with a guessable
                    // password. Set Seed:AdminPassword for the first start, then remove it.
                    logger.LogWarning("Seed admin {AdminEmail} does not exist and Seed:AdminPassword is not set; skipping admin creation.", adminEmail);
                    return;
                }

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
                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException("Failed to create admin user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }

                // Email confirmation is not required at sign-up, so anyone could register
                // with the admin address. Only promote an account whose email is confirmed
                // (the account created above, or one confirmed by an existing admin).
                if (!adminUser.EmailConfirmed)
                {
                    logger.LogWarning("Seed admin {AdminEmail} exists but its email is not confirmed; not granting Admin.", adminEmail);
                    return;
                }

                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
