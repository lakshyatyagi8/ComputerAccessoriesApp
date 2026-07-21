using Accessories.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Accessories.Api.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Find the existing database registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions));

            // Remove it so we don't use the real database
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add a new SQLite database specifically for testing
            // Using a unique Guid ensures parallel test runs don't lock the same file
            var dbName = $"integration_test_{Guid.NewGuid()}.db";
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dbName}");
            });

            // Build the service provider and ensure the test database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated(); // Applies schema without needing migrations
        });
    }
}