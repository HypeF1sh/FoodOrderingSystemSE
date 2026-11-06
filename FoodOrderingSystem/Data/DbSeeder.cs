using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Data;

public static class DbSeeder
{
    public const string AdminRole = "Administrator";
    public const string StaffRole = "Staff";
    public const string CustomerRole = "Customer";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
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

        // 3. Dining Areas & Restaurant Tables (2 areas, 8 tables)
        if (!await context.DiningAreas.AnyAsync())
        {
            var mainHall = new DiningArea
            {
                Name = "Main Dining Hall",
                Description = "Indoor air-conditioned dining area suitable for casual and family meals."
            };
            var gardenPatio = new DiningArea
            {
                Name = "Garden Patio",
                Description = "Al fresco outdoor seating surrounded by greenery."
            };

            context.DiningAreas.AddRange(mainHall, gardenPatio);
            await context.SaveChangesAsync();

            var tables = new List<RestaurantTable>
            {
                new() { DiningAreaId = mainHall.DiningAreaId, TableNumber = "M1", Capacity = 2, IsActive = true },
                new() { DiningAreaId = mainHall.DiningAreaId, TableNumber = "M2", Capacity = 2, IsActive = true },
                new() { DiningAreaId = mainHall.DiningAreaId, TableNumber = "M3", Capacity = 4, IsActive = true },
                new() { DiningAreaId = mainHall.DiningAreaId, TableNumber = "M4", Capacity = 4, IsActive = true },
                new() { DiningAreaId = mainHall.DiningAreaId, TableNumber = "M5", Capacity = 6, IsActive = true },
                new() { DiningAreaId = gardenPatio.DiningAreaId, TableNumber = "G1", Capacity = 2, IsActive = true },
                new() { DiningAreaId = gardenPatio.DiningAreaId, TableNumber = "G2", Capacity = 4, IsActive = true },
                new() { DiningAreaId = gardenPatio.DiningAreaId, TableNumber = "G3", Capacity = 8, IsActive = true }
            };

            context.RestaurantTables.AddRange(tables);
            await context.SaveChangesAsync();
        }

        // 4. Operating Hours (7 days: 0 = Sunday to 6 = Saturday)
        if (!await context.OperatingHours.AnyAsync())
        {
            var hours = new List<OperatingHours>();
            for (int day = 0; day <= 6; day++)
            {
                hours.Add(new OperatingHours
                {
                    DayOfWeek = day,
                    OpenTime = new TimeOnly(10, 0),
                    CloseTime = (day == 5 || day == 6) ? new TimeOnly(23, 0) : new TimeOnly(22, 0),
                    IsClosed = false
                });
            }

            context.OperatingHours.AddRange(hours);
            await context.SaveChangesAsync();
        }

        // 5. Menu Categories (4 categories) & Menu Items (15 items)
        if (!await context.MenuCategories.AnyAsync())
        {
            var starters = new MenuCategory { Name = "Starters & Appetizers", Description = "Light bites to begin your meal.", DisplayOrder = 1, IsActive = true };
            var mains = new MenuCategory { Name = "Main Courses", Description = "Hearty signature dishes prepared fresh.", DisplayOrder = 2, IsActive = true };
            var desserts = new MenuCategory { Name = "Desserts", Description = "Sweet handcrafted treats.", DisplayOrder = 3, IsActive = true };
            var beverages = new MenuCategory { Name = "Beverages", Description = "Refreshing drinks, coffee, and artisan teas.", DisplayOrder = 4, IsActive = true };

            context.MenuCategories.AddRange(starters, mains, desserts, beverages);
            await context.SaveChangesAsync();

            var items = new List<MenuItem>
            {
                // Starters (4)
                new() { MenuCategoryId = starters.MenuCategoryId, Name = "Garlic Herb Bruschetta", Description = "Toasted artisan sourdough topped with vine tomatoes, basil, and extra virgin olive oil.", Price = 14.90m, IsAvailable = true },
                new() { MenuCategoryId = starters.MenuCategoryId, Name = "Crispy Calamari Rings", Description = "Golden fried squid rings served with house-made lemon aioli.", Price = 18.50m, IsAvailable = true },
                new() { MenuCategoryId = starters.MenuCategoryId, Name = "Truffle Mushroom Soup", Description = "Creamy wild forest mushroom veloute drizzled with white truffle oil.", Price = 16.00m, IsAvailable = true },
                new() { MenuCategoryId = starters.MenuCategoryId, Name = "Caesar Salad", Description = "Crisp romaine lettuce, parmesan shavings, herb croutons, and classic Caesar dressing.", Price = 15.50m, IsAvailable = true },

                // Mains (5)
                new() { MenuCategoryId = mains.MenuCategoryId, Name = "Pan-Seared Atlantic Salmon", Description = "Served with asparagus spears, mashed potatoes, and lemon butter caper sauce.", Price = 38.90m, IsAvailable = true },
                new() { MenuCategoryId = mains.MenuCategoryId, Name = "Chargrilled Ribeye Steak", Description = "250g grass-fed ribeye with roasted garlic, seasonal vegetables, and black pepper jus.", Price = 52.00m, IsAvailable = true },
                new() { MenuCategoryId = mains.MenuCategoryId, Name = "Herb-Roasted Chicken Chop", Description = "Boneless chicken thigh marinated in rosemary and thyme with mushroom gravy.", Price = 26.90m, IsAvailable = true },
                new() { MenuCategoryId = mains.MenuCategoryId, Name = "Seafood Aglio Olio", Description = "Spaghetti tossed with tiger prawns, mussels, garlic, chili flakes, and olive oil.", Price = 29.50m, IsAvailable = true },
                new() { MenuCategoryId = mains.MenuCategoryId, Name = "Classic Wagyu Beef Burger", Description = "Brioche bun, aged cheddar, caramelized onions, house pickles, and truffle fries.", Price = 32.00m, IsAvailable = true },

                // Desserts (3)
                new() { MenuCategoryId = desserts.MenuCategoryId, Name = "Classic Tiramisu", Description = "Espresso-soaked savoiardi biscuits layered with mascarpone cream and cocoa.", Price = 16.90m, IsAvailable = true },
                new() { MenuCategoryId = desserts.MenuCategoryId, Name = "Molten Chocolate Lava Cake", Description = "Warm dark chocolate cake with a gooey center, served with vanilla bean gelato.", Price = 18.90m, IsAvailable = true },
                new() { MenuCategoryId = desserts.MenuCategoryId, Name = "New York Cheesecake", Description = "Rich baked cheesecake topped with mixed berry compote.", Price = 15.90m, IsAvailable = true },

                // Beverages (3)
                new() { MenuCategoryId = beverages.MenuCategoryId, Name = "Iced Passionfruit Sparkling Tea", Description = "Cold-brewed jasmine green tea infused with passionfruit puree and mint.", Price = 11.50m, IsAvailable = true },
                new() { MenuCategoryId = beverages.MenuCategoryId, Name = "Artisan Flat White", Description = "Double shot espresso with velvety steamed micro-foam milk.", Price = 10.90m, IsAvailable = true },
                new() { MenuCategoryId = beverages.MenuCategoryId, Name = "Freshly Squeezed Orange Juice", Description = "100% pure Valencia oranges, chilled with no added sugar.", Price = 12.00m, IsAvailable = true }
            };

            context.MenuItems.AddRange(items);
            await context.SaveChangesAsync();
        }

        // 6. Photos for the seeded dishes. The files are committed in wwwroot/images/menu, so every PC has them.
        //    This runs on every start (not only on a new database), so teammates' existing databases get the
        //    photos too. It only fills dishes that have no photo yet: an image the admin uploaded is never replaced.
        var dishesWithoutPhoto = await context.MenuItems
            .Where(m => m.ImageUrl == "")
            .ToListAsync();

        foreach (var dish in dishesWithoutPhoto)
        {
            if (SeedDishPhotos.TryGetValue(dish.Name, out var fileName))
                dish.ImageUrl = $"/images/menu/{fileName}";
        }

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync();
    }

    // Seeded dish name -> its photo in wwwroot/images/menu (matched by name, because ids differ between databases)
    private static readonly Dictionary<string, string> SeedDishPhotos = new()
    {
        ["Garlic Herb Bruschetta"] = "garlic-herb-bruschetta.jpg",
        ["Crispy Calamari Rings"] = "crispy-calamari-rings.jpg",
        ["Truffle Mushroom Soup"] = "truffle-mushroom-soup.jpg",
        ["Caesar Salad"] = "caesar-salad.jpg",
        ["Pan-Seared Atlantic Salmon"] = "pan-seared-atlantic-salmon.jpg",
        ["Chargrilled Ribeye Steak"] = "chargrilled-ribeye-steak.jpg",
        ["Herb-Roasted Chicken Chop"] = "herb-roasted-chicken-chop.jpg",
        ["Seafood Aglio Olio"] = "seafood-aglio-olio.jpg",
        ["Classic Wagyu Beef Burger"] = "classic-wagyu-beef-burger.jpg",
        ["Classic Tiramisu"] = "classic-tiramisu.jpg",
        ["Molten Chocolate Lava Cake"] = "molten-chocolate-lava-cake.jpg",
        ["New York Cheesecake"] = "new-york-cheesecake.jpg",
        ["Iced Passionfruit Sparkling Tea"] = "iced-passionfruit-sparkling-tea.jpg",
        ["Artisan Flat White"] = "artisan-flat-white.jpg",
        ["Freshly Squeezed Orange Juice"] = "freshly-squeezed-orange-juice.jpg"
    };

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