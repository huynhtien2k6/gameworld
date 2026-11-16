using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Services;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        if (configuration.GetValue("Database:Initialize", true))
        {
            var useSqlServer = string.Equals(
                configuration["Database:Provider"],
                "SqlServer",
                StringComparison.OrdinalIgnoreCase);

            if (useSqlServer)
                await db.Database.MigrateAsync();
            else
                await db.Database.EnsureCreatedAsync();
        }

        var password = configuration["BootstrapAdmin:Password"];
        var username = configuration["BootstrapAdmin:Username"]?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(password))
        {
            if (password.Length < 12 || string.IsNullOrEmpty(username) || username.Length > 50)
                throw new InvalidOperationException("Bootstrap admin needs a username and password of at least 12 characters.");
            if (!await db.AdminUsers.AnyAsync(x => x.Username == username))
            {
                var admin = new AdminUser { Username = username, Role = "Admin" };
                admin.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(admin, password);
                db.AdminUsers.Add(admin); await db.SaveChangesAsync();
            }
        }
    }
}
