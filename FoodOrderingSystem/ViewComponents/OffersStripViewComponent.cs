using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.ViewComponents;

// A strip of current announcements and promotions. Add it to any page with:
//     @await Component.InvokeAsync("OffersStrip")
public class OffersStripViewComponent : ViewComponent
{
    private readonly IAnnouncementService _announcementService;
    private readonly IPromotionService _promotionService;

    public OffersStripViewComponent(IAnnouncementService announcementService, IPromotionService promotionService)
    {
        _announcementService = announcementService;
        _promotionService = promotionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var announcements = (await _announcementService.GetActiveAsync()).Take(2).ToList();
        var promotions = (await _promotionService.GetActiveAsync()).Take(3).ToList();
        return View((announcements, promotions));
    }
}
