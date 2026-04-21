using backend.Models.Entities;
using backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.DisplayName).HasMaxLength(120);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.SellerId);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasOne(p => p.Seller).WithMany(u => u.Products).HasForeignKey(p => p.SellerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.BuyerId);
            entity.HasIndex(o => o.CreatedAtUtc);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.BuyerCancellationReason).HasMaxLength(500);
            entity.Property(o => o.CancellationReviewerNote).HasMaxLength(500);
            entity.Property(o => o.SimulatedPaymentMethod).HasMaxLength(32);
            entity.HasOne(o => o.Buyer).WithMany(u => u.Orders).HasForeignKey(o => o.BuyerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(ol => ol.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(ol => ol.Order).WithMany(o => o.Lines).HasForeignKey(ol => ol.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ol => ol.Product).WithMany(p => p.OrderLines).HasForeignKey(ol => ol.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
