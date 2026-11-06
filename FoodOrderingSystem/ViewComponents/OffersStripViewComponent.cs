using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.ViewComponents;

// Current announcements and promotions. Add it to a page with, for example:
//     @await Component.InvokeAsync("OffersStrip", new { compact = true })                 (home page)
//     @await Component.InvokeAsync("OffersStrip", new { showAnnouncements = false })      (menu page)
public class OffersStripViewComponent : ViewComponent
{
    private readonly IAnnouncementService _announcementService;
    private readonly IPromotionService _promotionService;

    public OffersStripViewComponent(IAnnouncementService announcementService, IPromotionService promotionService)
    {
        _announcementService = announcementService;
        _promotionService = promotionService;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        bool showAnnouncements = true, bool showPromotions = true, bool compact = false)
    {
        var announcements = showAnnouncements
            ? (await _announcementService.GetActiveAsync()).Take(5).ToList()
            : new List<Announcement>();

        var promotions = showPromotions
            ? (await _promotionService.GetActiveAsync()).Take(5).ToList()
            : new List<Promotion>();

        return View((announcements, promotions, compact));
    }
}
