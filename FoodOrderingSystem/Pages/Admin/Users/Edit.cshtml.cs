using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodOrderingSystem.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly IUserAdminService _userAdminService;

    public EditModel(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public UserListItemViewModel? TargetUser { get; set; }
    public List<SelectListItem> RoleOptions { get; set; } = new();
    public bool IsEditingSelf { get; set; }

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        if (!await LoadAsync(id)) return NotFound();

        Input = new InputModel
        {
            Id = TargetUser!.Id,
            Role = TargetUser.Role,
            IsActive = TargetUser.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await LoadAsync(Input.Id)) return NotFound();

        if (!ModelState.IsValid) return Page();

        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var (success, message) = await _userAdminService.UpdateUserAsync(
            Input.Id, Input.Role, Input.IsActive, currentAdminId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        SuccessMessage = message;
        return RedirectToPage("Index");
    }

    private async Task<bool> LoadAsync(string id)
    {
        TargetUser = await _userAdminService.GetUserAsync(id);
        if (TargetUser == null) return false;

        RoleOptions = _userAdminService.GetAvailableRoles()
            .Select(r => new SelectListItem(r, r))
            .ToList();

        IsEditingSelf = TargetUser.Id == User.FindFirstValue(ClaimTypes.NameIdentifier);
        return true;
    }
}
