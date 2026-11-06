// PB07 (Carynn) - system announcement shown to customers (table owned by A)
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models;

public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}