// PB15/PB17 (Jordan) - view model: hero photo view model
namespace FoodOrderingSystem.ViewModels;

// One slide of the home page slideshow: the web path of the photo and its credit line ("Name / Unsplash")
public record HeroPhotoViewModel(string Url, string Credit);
