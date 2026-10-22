namespace FoodOrderingSystem.Services;

public class ImageStorageService : IImageStorageService
{
    // Uploads live in their own folder so they can be git-ignored and never mix with site assets
    private const string UploadFolder = "uploads/menu";
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly IWebHostEnvironment _environment;

    public ImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string PlaceholderImageUrl => "/images/menu-placeholder.svg";

    public string? ValidateImage(IFormFile file)
    {
        // 1. Reject empty and oversized files
        if (file.Length == 0)
            return "The selected image is empty.";
        if (file.Length > MaxFileSizeBytes)
            return "Images must be 2 MB or smaller.";

        // 2. Check both the extension and the content type the browser reported,
        //    so a renamed .exe or .txt is refused
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG or WebP images are allowed.";

        return null;
    }

    public async Task<string> SaveMenuImageAsync(IFormFile file)
    {
        // 1. Make sure wwwroot/uploads/menu exists
        var folderPath = Path.Combine(_environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(folderPath);

        // 2. Use a GUID as the file name: the user's own file name never reaches the disk,
        //    so it cannot overwrite another file or contain a path like "../../"
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";

        // 3. Copy the upload to disk
        await using var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.CreateNew);
        await file.CopyToAsync(stream);

        // 4. Return the web path stored in MenuItem.ImageUrl
        return $"/{UploadFolder}/{fileName}";
    }

    public void DeleteMenuImage(string? imageUrl)
    {
        // Only files inside our upload folder are ever deleted (never the placeholder or other assets)
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith($"/{UploadFolder}/"))
            return;

        // Path.GetFileName strips any directory part, as a second guard against path tricks
        var filePath = Path.Combine(_environment.WebRootPath, UploadFolder, Path.GetFileName(imageUrl));
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
