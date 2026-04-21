using backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding roles and demo users.");

        var roles = new[]
        {
            new Role { Name = "Buyer" },
            new Role { Name = "Seller" },
            new Role { Name = "Admin" },
        };

        await context.Roles.AddRangeAsync(roles, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        async Task<User> CreateUserAsync(string email, string displayName, string password, params string[] roleNames)
        {
            var user = new User
            {
                Email = email,
                DisplayName = displayName,
                CreatedAtUtc = DateTime.UtcNow,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);

            foreach (var roleName in roleNames)
            {
                var role = await context.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
                user.UserRoles.Add(new UserRole { RoleId = role.Id });
            }

            await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return user;
        }

        var seller = await CreateUserAsync("seller@demo.local", "ร้านตัวอย่าง", "Seller123!", "Seller");
        await CreateUserAsync("buyer@demo.local", "ผู้ซื้อตัวอย่าง", "Buyer123!", "Buyer");
        await CreateUserAsync("admin@demo.local", "ผู้ดูแลระบบ", "Admin123!", "Admin");

        var products = new[]
        {
            new Product
            {
                SellerId = seller.Id,
                Name = "เมาส์ไร้สาย",
                Description = "เมาส์ไร้สายจับถนัดมือ เหมาะใช้งานทั่วไป",
                Price = 390m,
                StockQuantity = 50,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new Product
            {
                SellerId = seller.Id,
                Name = "สาย USB-C ถัก",
                Description = "สาย USB-C ยาว 1 เมตร หุ้มถัก",
                Price = 129m,
                StockQuantity = 200,
                CreatedAtUtc = DateTime.UtcNow,
            },
        };

        await context.Products.AddRangeAsync(products, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Demo accounts: buyer@demo.local / Buyer123!, seller@demo.local / Seller123!, admin@demo.local / Admin123!");
    }
}
