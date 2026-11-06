// UI (Jordan) - landing page: photo slideshow, signature dishes, hours, offers
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages
{
    // Landing page for people arriving from a link, Google or social media (a table QR scan goes to the Menu instead).
    // Everything shown comes from the existing services: hours, menu, announcements, promotions and settings.
    public class IndexModel : PageModel
    {
        // Photos for the hero slideshow live in wwwroot/images/landing/ as hero-1.jpg ... hero-5.jpg (or .webp).
        // Missing files are skipped; with none at all the hero shows soft coloured slides instead.
        private const string HeroFolder = "images/landing";
        private const int MaxHeroPhotos = 5;
        private const int SignatureDishCount = 8;   // enough cards for the drifting row to feel full

        // Carynn's services (Domain A) - we read through their interfaces, never the tables directly
        private readonly IOperatingHoursService _operatingHoursService;
        private readonly IAnnouncementService _announcementService;
        private readonly IPromotionService _promotionService;
        private readonly IRestaurantSettingService _settingService;
        private readonly IMenuService _menuService;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(
            IOperatingHoursService operatingHoursService,
            IAnnouncementService announcementService,
            IPromotionService promotionService,
            IRestaurantSettingService settingService,
            IMenuService menuService,
            IWebHostEnvironment environment)
        {
            _operatingHoursService = operatingHoursService;
            _announcementService = announcementService;
            _promotionService = promotionService;
            _settingService = settingService;
            _menuService = menuService;
            _environment = environment;
        }

        public List<OperatingHoursDayViewModel> OpeningHours { get; set; } = new();
        public OperatingHoursDayViewModel? Today { get; set; }
        public List<HeroPhotoViewModel> HeroPhotos { get; set; } = new();
        public List<MenuItemCardViewModel> SignatureDishes { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
        public List<Promotion> Promotions { get; set; } = new();

        // Optional settings an admin can add under Configurations > Settings; hidden when not set
        public string? Address { get; set; }
        public string? Phone { get; set; }

        public async Task OnGetAsync()
        {
            var week = await _operatingHoursService.GetWeekAsync();

            // The service returns Sunday first (0); show Monday first, as people read a week
            OpeningHours = week.OrderBy(d => (d.DayOfWeek + 6) % 7).ToList();
            Today = week.FirstOrDefault(d => d.DayOfWeek == (int)DateTime.Today.DayOfWeek);

            HeroPhotos = FindHeroPhotos();

            // Signature dishes: available dishes taken in turn from each category (starter, main, dessert, drink,
            // then the second of each...), so the row shows the range of the menu
            var menu = await _menuService.GetMenuForBrowseAsync(null, null);
            SignatureDishes = menu.Sections
                .SelectMany(s => s.Items.Where(i => i.IsAvailable).Select((item, position) => (item, position)))
                .OrderBy(x => x.position)
                .Select(x => x.item)
                .Take(SignatureDishCount)
                .ToList();

            Announcements = await _announcementService.GetActiveAsync();

            // Only promotions that are switched on and running today
            var now = DateTime.Now;
            Promotions = (await _promotionService.GetAllAsync())
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .OrderBy(p => p.EndDate)
                .ToList();

            var settings = await _settingService.GetAllAsync();
            Address = settings.FirstOrDefault(s => s.Key.Equals("Address", StringComparison.OrdinalIgnoreCase))?.Value;
            Phone = settings.FirstOrDefault(s => s.Key.Equals("Phone", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        // hero-1 ... hero-5, .webp preferred over .jpg, each with its credit from credits.json (file name -> "Name / Unsplash")
        private List<HeroPhotoViewModel> FindHeroPhotos()
        {
            var files = _environment.WebRootFileProvider;
            var credits = new Dictionary<string, string>();
            var creditsFile = files.GetFileInfo($"{HeroFolder}/credits.json");
            if (creditsFile.Exists)
            {
                using var stream = creditsFile.CreateReadStream();
                credits = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? credits;
            }

            var photos = new List<HeroPhotoViewModel>();
            for (var i = 1; i <= MaxHeroPhotos; i++)
            {
                foreach (var extension in new[] { "webp", "jpg", "jpeg", "png" })
                {
                    var fileName = $"hero-{i}.{extension}";
                    if (files.GetFileInfo($"{HeroFolder}/{fileName}").Exists)
                    {
                        photos.Add(new HeroPhotoViewModel($"/{HeroFolder}/{fileName}", credits.GetValueOrDefault(fileName, string.Empty)));
                        break;
                    }
                }
            }
            return photos;
        }
    }
}
