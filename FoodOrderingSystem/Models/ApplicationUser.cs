// PB01 (Carynn) - Identity user + FullName
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    // Deactivated users cannot log in (PB04)
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
