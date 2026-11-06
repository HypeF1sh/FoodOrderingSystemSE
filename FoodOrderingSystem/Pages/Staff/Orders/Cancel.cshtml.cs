// PB15 (Jordan) - cancel an order (Pending or Preparing only)
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Orders;

// Confirmation page for cancelling an order (destructive actions get their own page)
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class CancelModel : PageModel
{
    private readonly IOrderService _orderService;

    public CancelModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public StaffOrderViewModel? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Order = await _orderService.GetOrderForStaffAsync(id);
        if (Order == null) return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, OrderStatus currentStatus)
    {
        // The service checks the cancel rule and that nobody changed the order in the meantime
        var (success, message) = await _orderService.CancelOrderAsync(id, currentStatus);
        if (!success)
        {
            Order = await _orderService.GetOrderForStaffAsync(id);
            if (Order == null) return NotFound();

            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        TempData["SuccessMessage"] = message;
        return RedirectToPage("./Index");
    }
}
