// PB15/PB17 (Jordan) - view model: floor view model
namespace FoodOrderingSystem.ViewModels;

// Everything the staff Floor page shows for one dining area
public class FloorViewModel
{
    // How many chips fit on the Needs-you bar; the rest are counted in "+N more"
    public const int BarLimit = 5;

    public List<DiningAreaOptionViewModel> Areas { get; set; } = new();
    public int? SelectedAreaId { get; set; }
    public string SelectedAreaName { get; set; } = string.Empty;
    public List<FloorTableViewModel> Tables { get; set; } = new();
    public List<NeedsYouItemViewModel> NeedsYou { get; set; } = new();   // every area, most urgent first
    public DateTime UpdatedAt { get; set; }

    // Set by the page from ?table=, so the grid can outline the open table and the drawer can show it
    public int? SelectedTableId { get; set; }
    public FloorTableViewModel? SelectedTable => Tables.FirstOrDefault(t => t.RestaurantTableId == SelectedTableId);
}
