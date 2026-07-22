using Microsoft.AspNetCore.Identity;
using CafeManagement.Constants;
using CafeManagement.Models.Entities;

namespace CafeManagement.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            var roles = new[] { "Admin", "Manager", "Cashier", "Waiter", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByEmailAsync("admin@cafe.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@cafe.com",
                    UserName = "admin",
                    FullName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new[]
                {
                    new Category { Name = "Coffee", Description = "Các loại cà phê", Icon = "fa-coffee", DisplayOrder = 1 },
                    new Category { Name = "Tea", Description = "Các loại trà", Icon = "fa-leaf", DisplayOrder = 2 },
                    new Category { Name = "Juice", Description = "Các loại nước ép", Icon = "fa-glass", DisplayOrder = 3 },
                    new Category { Name = "Snack", Description = "Các loại tấm", Icon = "fa-cake", DisplayOrder = 4 },
                    new Category { Name = "Dessert", Description = "Tráng miệng", Icon = "fa-ice-cream", DisplayOrder = 5 }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed Products
            if (!context.Products.Any())
            {
                var coffeeCategory = context.Categories.FirstOrDefault(c => c.Name == "Coffee");
                if (coffeeCategory != null)
                {
                    var products = new[]
                    {
                        new Product 
                        { 
                            Name = "Espresso", 
                            Description = "Cà phê espresso đậm đà", 
                            Price = 25000, 
                            CategoryId = coffeeCategory.Id, 
                            Quantity = 100, 
                            IsAvailable = true 
                        },
                        new Product 
                        { 
                            Name = "Americano", 
                            Description = "Cà phê Americano", 
                            Price = 30000, 
                            CategoryId = coffeeCategory.Id, 
                            Quantity = 100, 
                            IsAvailable = true 
                        },
                        new Product 
                        { 
                            Name = "Cappuccino", 
                            Description = "Cà phê Cappuccino", 
                            Price = 35000, 
                            CategoryId = coffeeCategory.Id, 
                            Quantity = 100, 
                            IsAvailable = true 
                        }
                    };

                    context.Products.AddRange(products);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Tables
            if (!context.RestaurantTables.Any())
            {
                var tables = new List<RestaurantTable>();
                for (int i = 1; i <= 10; i++)
                {
                    tables.Add(new RestaurantTable
                    {
                        TableCode = $"T{i:D2}",
                        Capacity = i % 2 == 0 ? 4 : 2,
                        Status = "Available",
                        Location = $"Khu {(i - 1) / 5 + 1}"
                    });
                }

                context.RestaurantTables.AddRange(tables);
                await context.SaveChangesAsync();
            }
        }
    }
}
