using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KemisOnboarding.Api.Models;

namespace KemisOnboarding.Api.Data;

public static class DevSeeder
{
    // Development only. Password for all of these: Kemis@12345
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher<AppUser> hasher)
    {
        if (await db.Users.AnyAsync()) return;

        var seeds = new[]
        {
            ("Dev Admin", "admin@kemis.local", AppRoles.Admin),
            ("Dev CDE", "cde@kemis.local", AppRoles.CDE),
            ("Dev SCDE", "scde@kemis.local", AppRoles.SCDE),
            ("Dev QASO", "qaso@kemis.local", AppRoles.QASO),
            ("Dev CQASO", "cqaso@kemis.local", AppRoles.CQASO)
        };

        foreach (var (name, email, role) in seeds)
        {
            var user = new AppUser { FullName = name, Email = email, Role = role };
            user.PasswordHash = hasher.HashPassword(user, "Kemis@12345");
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }
}