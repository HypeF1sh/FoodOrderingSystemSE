using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Data;

public static class DbSeeder
{
    public const string AdminRole = "Administrator";
    public const string StaffRole = "Staff";
    public const string CustomerRole = "Customer";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Roles
        foreach (var role in new[] { AdminRole, StaffRole, CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. One dev account per role (development only)
        await CreateUserAsync(userManager, "admin@rroms.com", "Admin User", "Admin@123", AdminRole);
        await CreateUserAsync(userManager, "staff@rroms.com", "Staff User", "Staff@123", StaffRole);
        await CreateUserAsync(userManager, "customer@rroms.com", "Customer User", "Customer@123", CustomerRole);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string fullName, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
        else
            throw new InvalidOperationException(
                $"Seeding {email} failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
