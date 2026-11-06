// Data layer (Wallace) - EF Core database context: one DbSet per table, enum-as-text and delete rules
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Data;

// Inherits IdentityDbContext so the Identity tables (users, roles) live in the same database
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // One DbSet per table; only the Data layer and services use these, never a page
    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OperatingHours> OperatingHours => Set<OperatingHours>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<RestaurantSetting> RestaurantSettings => Set<RestaurantSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity must configure its own tables first
        base.OnModelCreating(builder);

        // Store enums as strings
        builder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Payment>()
            .Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Restrict = cannot delete a user who still has bookings/orders (history is kept)
        // Prevent SQL Server multiple cascade path cycles
        builder.Entity<Reservation>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Reservation)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.ReservationId)
            // SetNull = deleting a booking keeps its orders, just unlinks them
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Payment>()
            .HasOne(p => p.RecordedByUser)
            .WithMany()
            .HasForeignKey(p => p.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Feedback>()
            .HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Feedback>()
            .HasOne(f => f.Reservation)
            .WithMany()
            .HasForeignKey(f => f.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Feedback>()
            .HasOne(f => f.Order)
            .WithMany()
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}