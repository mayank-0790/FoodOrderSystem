using FoodOrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique Email Constraint
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 1-to-1 Relationship: Order <-> Payment
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent SQL Server multiple cascade path conflicts on OrderItems & CartItems
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany(m => m.OrderItems)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.MenuItem)
            .WithMany(m => m.CartItems)
            .HasForeignKey(ci => ci.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Initial Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Starters", Description = "Appetizers and crispy bites", IsActive = true },
            new Category { CategoryId = 2, Name = "Main Course", Description = "Signature curries, bowls, and entrees", IsActive = true },
            new Category { CategoryId = 3, Name = "Beverages", Description = "Fresh shakes, mocktails, and sodas", IsActive = true },
            new Category { CategoryId = 4, Name = "Desserts", Description = "Sweet treats and ice creams", IsActive = true }
        );
    }
}