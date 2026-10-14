using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly IUserAdminService _userAdminService;

    public IndexModel(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    public List<UserListItemViewModel> Users { get; set; } = new();

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Users = await _userAdminService.GetAllUsersAsync();
    }
}
