using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CommunityShareStack.Data;

namespace CommunityShareStack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var adminEmail = configuration["Seed:AdminEmail"];
            // Only used to create the admin account the first time. Supply it via user
            // secrets locally or the Seed__AdminPassword app setting in Azure; never
            // commit it to appsettings.json.
            var adminPassword = configuration["Seed:AdminPassword"];
            SeedData.InitializeAsync(host.Services, adminEmail, adminPassword).GetAwaiter().GetResult();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
