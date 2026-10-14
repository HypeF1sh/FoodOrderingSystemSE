using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Services;

public class UserAdminService : IUserAdminService
{
    private static readonly string[] Roles =
    {
        DbSeeder.AdminRole, DbSeeder.StaffRole, DbSeeder.CustomerRole
    };

    private readonly UserManager<ApplicationUser> _userManager;

    // UserManager only: pages never touch the DbContext directly (3-tier rule)
    public UserAdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IReadOnlyList<string> GetAvailableRoles() => Roles;

    public async Task<List<UserListItemViewModel>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var result = new List<UserListItemViewModel>();

        foreach (var user in users)
            result.Add(await ToViewModelAsync(user));

        return result;
    }

    public async Task<UserListItemViewModel?> GetUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : await ToViewModelAsync(user);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(
        string userId, string newRole, bool isActive, string currentAdminId)
    {
        if (!Roles.Contains(newRole))
            return (false, "Invalid role selected.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        // Rule: an admin cannot demote or deactivate themselves
        if (user.Id == currentAdminId && (newRole != DbSeeder.AdminRole || !isActive))
            return (false, "You cannot demote or deactivate your own account.");

        // Change role: remove old roles, add the new one
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(newRole) || currentRoles.Count != 1)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return (false, "Could not remove the old role.");

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
                return (false, "Could not assign the new role.");
        }

        // Change active status
        var wasActive = user.IsActive;
        user.IsActive = isActive;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, "Could not update the user.");

        // Invalidate any existing login session when a user is deactivated
        if (wasActive && !isActive)
            await _userManager.UpdateSecurityStampAsync(user);

        return (true, $"{user.FullName} was updated successfully.");
    }

    private async Task<UserListItemViewModel> ToViewModelAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "(none)",
            IsActive = user.IsActive
        };
    }
}
