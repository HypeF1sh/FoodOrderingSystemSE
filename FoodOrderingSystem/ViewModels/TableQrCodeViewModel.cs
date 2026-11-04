namespace FoodOrderingSystem.ViewModels;

// One table card on the printable QR page (PB14 table QR ordering)
public class TableQrCodeViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string DiningAreaName { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;

    // PNG image as a data: URI, so the page needs no extra request per table
    public string ImageDataUri { get; set; } = string.Empty;
}
