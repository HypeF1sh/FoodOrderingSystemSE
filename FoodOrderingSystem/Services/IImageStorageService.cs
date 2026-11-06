// PB06 (Jordan) - dish photo storage (interface)
namespace FoodOrderingSystem.Services;

public interface IImageStorageService
{
    /// <summary>Web path of the image shown when a menu item has no upload.</summary>
    string PlaceholderImageUrl { get; }

    /// <summary>Checks type and size of an uploaded image. Returns null if it is valid, otherwise an error message.</summary>
    string? ValidateImage(IFormFile file);

    /// <summary>Saves a validated image under a new random file name and returns its web path.</summary>
    Task<string> SaveMenuImageAsync(IFormFile file);

    /// <summary>Deletes a previously uploaded menu image. Ignores empty paths and anything outside the upload folder.</summary>
    void DeleteMenuImage(string? imageUrl);
}
