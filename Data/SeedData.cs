using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository;
using System.Security.Cryptography;
using System.Text;

namespace Quản_lý_quán_cafe.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            try
            {
                // Create database if not exists
                await context.Database.EnsureCreatedAsync();

                // Seed Roles
                await SeedRolesAsync(context);

                // Seed Employees
                await SeedEmployeesAsync(context);

                // Seed Users
                await SeedUsersAsync(context);

                // Seed Categories
                await SeedCategoriesAsync(context);

                // Seed Products
                await SeedProductsAsync(context);

                // Seed Customers
                await SeedCustomersAsync(context);

                // Seed RestaurantTables
                await SeedRestaurantTablesAsync(context);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seed database error: {ex.Message}");
                throw;
            }
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            if (await context.Roles.AnyAsync())
                return;

            var roles = new List<Role>
            {
                new Role
                {
                    RoleName = "Admin",
                    Description = "Quản trị viên hệ thống",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Role
                {
                    RoleName = "Cashier",
                    Description = "Thu ngân quán café",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Role
                {
                    RoleName = "Customer",
                    Description = "Khách hàng",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        private static async Task SeedEmployeesAsync(ApplicationDbContext context)
        {
            if (await context.Employees.AnyAsync())
                return;

            var employees = new List<Employee>
            {
                new Employee
                {
                    FullName = "Quản trị viên",
                    Gender = "Male",
                    Email = "admin@cafe.com",
                    Phone = "0123456789",
                    Address = "123 Main Street",
                    HireDate = DateTime.UtcNow.AddYears(-1),
                    Salary = 10000000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Employee
                {
                    FullName = "Thu ngân",
                    Gender = "Female",
                    Email = "cashier@cafe.com",
                    Phone = "0123456790",
                    Address = "123 Main Street",
                    HireDate = DateTime.UtcNow.AddMonths(-6),
                    Salary = 5000000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Employees.AddRangeAsync(employees);
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            if (await context.Users.AnyAsync())
                return;

            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var cashierRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Cashier");
            var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");

            var adminEmployee = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "Quản trị viên");
            var cashierEmployee = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "Thu ngân");

            var hashedPassword = HashPassword("123456");

            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = hashedPassword,
                    RoleID = adminRole?.RoleID ?? 1,
                    EmployeeID = adminEmployee?.EmployeeID,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new User
                {
                    Username = "cashier",
                    PasswordHash = hashedPassword,
                    RoleID = cashierRole?.RoleID ?? 2,
                    EmployeeID = cashierEmployee?.EmployeeID,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new User
                {
                    Username = "customer",
                    PasswordHash = hashedPassword,
                    RoleID = customerRole?.RoleID ?? 3,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            if (await context.Categories.AnyAsync())
                return;

            var categories = new List<Category>
            {
                new Category
                {
                    CategoryName = "Cà Phê",
                    Description = "Các loại cà phê khác nhau",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Trà",
                    Description = "Các loại trà thơm ngon",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Bánh",
                    Description = "Các loại bánh mặn và bánh ngọt",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Khác",
                    Description = "Các sản phẩm khác",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            if (await context.Products.AnyAsync())
                return;

            var coffeeCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Cà Phê");
            var teaCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Trà");
            var cakeCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Bánh");

            var products = new List<Product>
            {
                new Product
                {
                    ProductName = "Espresso",
                    Description = "Cà phê Espresso đậm đà",
                    CategoryID = coffeeCategory?.CategoryID ?? 1,
                    Price = 30000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Cappuccino",
                    Description = "Cà phê Cappuccino thơm ngon",
                    CategoryID = coffeeCategory?.CategoryID ?? 1,
                    Price = 40000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Latte",
                    Description = "Cà phê Latte kem sữa",
                    CategoryID = coffeeCategory?.CategoryID ?? 1,
                    Price = 45000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Trà Đen",
                    Description = "Trà đen thơm ngon",
                    CategoryID = teaCategory?.CategoryID ?? 2,
                    Price = 25000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Trà Xanh",
                    Description = "Trà xanh tươi mát",
                    CategoryID = teaCategory?.CategoryID ?? 2,
                    Price = 25000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Bánh Mì",
                    Description = "Bánh mì tươi ngon",
                    CategoryID = cakeCategory?.CategoryID ?? 3,
                    Price = 15000,
                    Quantity = 50,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Bánh Kem",
                    Description = "Bánh kem hoa quả",
                    CategoryID = cakeCategory?.CategoryID ?? 3,
                    Price = 50000,
                    Quantity = 20,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCustomersAsync(ApplicationDbContext context)
        {
            if (await context.Customers.AnyAsync())
                return;

            var customers = new List<Customer>
            {
                new Customer
                {
                    CustomerName = "Khách hàng Demo",
                    Phone = "0987654321",
                    Email = "demo@customer.com",
                    Address = "123 Customer Street",
                    RewardPoints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        private static async Task SeedRestaurantTablesAsync(ApplicationDbContext context)
        {
            if (await context.RestaurantTables.AnyAsync())
                return;

            var tables = new List<RestaurantTable>();

            // Create 10 tables with 2, 4, or 6 seats
            for (int i = 1; i <= 10; i++)
            {
                int capacity = (i % 3 == 0) ? 6 : (i % 2 == 0) ? 4 : 2;
                tables.Add(new RestaurantTable
                {
                    TableNumber = $"T{i:D2}",
                    Capacity = capacity,
                    TableStatus = "Available",
                    Location = i <= 5 ? "Tầng 1" : "Tầng 2",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            await context.RestaurantTables.AddRangeAsync(tables);
            await context.SaveChangesAsync();
        }

        // Helper method to hash password
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
