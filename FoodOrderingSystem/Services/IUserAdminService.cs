// PB04 (Carynn) - user and role management (interface)
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

public interface IUserAdminService
{
    Task<List<UserListItemViewModel>> GetAllUsersAsync();
    Task<UserListItemViewModel?> GetUserAsync(string id);
    IReadOnlyList<string> GetAvailableRoles();

    /// <summary>Changes a user's role and active status. Returns (success, message).</summary>
    Task<(bool Success, string Message)> UpdateUserAsync(
        string userId, string newRole, bool isActive, string currentAdminId);
}
