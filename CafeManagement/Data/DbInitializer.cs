using CafeManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Data
{
    public static class DbInitializer
    {
        public const string RoleAdmin = "Admin";
        public const string RoleStaff = "Staff";
        public const string RoleCustomer = "Customer";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            // Dùng EnsureCreated để đơn giản hoá: tự tạo database + bảng theo Model
            // mà không cần chạy lệnh "dotnet ef migrations" trước.
            // Nếu muốn dùng Migrations chuyên nghiệp hơn, xem hướng dẫn trong README.md
            await context.Database.EnsureCreatedAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Tạo các role nếu chưa có
            foreach (var role in new[] { RoleAdmin, RoleStaff, RoleCustomer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@cafe.com";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên",
                    EmailConfirmed = true,
                    PhoneNumber = "0900000000"
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, RoleAdmin);
            }

            // 3. Tạo tài khoản Staff mẫu
            var staffEmail = "staff@cafe.com";
            if (await userManager.FindByEmailAsync(staffEmail) is null)
            {
                var staff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = "Nhân viên phục vụ",
                    EmailConfirmed = true,
                    PhoneNumber = "0900000001"
                };
                var result = await userManager.CreateAsync(staff, "Staff@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(staff, RoleStaff);
            }

            // 4. Danh mục + món mẫu
            if (!await context.Categories.AnyAsync())
            {
                var caPhe = new Category { Name = "Cà phê", Description = "Các loại cà phê" };
                var traSua = new Category { Name = "Trà sữa & Trà trái cây" };
                var banhNgot = new Category { Name = "Bánh ngọt" };
                context.Categories.AddRange(caPhe, traSua, banhNgot);
                await context.SaveChangesAsync();

                context.Products.AddRange(
                    new Product { Name = "Cà phê đen đá", Price = 25000, CategoryId = caPhe.Id },
                    new Product { Name = "Cà phê sữa đá", Price = 29000, CategoryId = caPhe.Id },
                    new Product { Name = "Bạc xỉu", Price = 32000, CategoryId = caPhe.Id },
                    new Product { Name = "Trà sữa trân châu", Price = 35000, CategoryId = traSua.Id },
                    new Product { Name = "Trà đào cam sả", Price = 39000, CategoryId = traSua.Id },
                    new Product { Name = "Bánh tiramisu", Price = 45000, CategoryId = banhNgot.Id },
                    new Product { Name = "Bánh croissant", Price = 30000, CategoryId = banhNgot.Id }
                );
                await context.SaveChangesAsync();
            }

            // 5. Bàn mẫu
            if (!await context.RestaurantTables.AnyAsync())
            {
                context.RestaurantTables.AddRange(
                    new RestaurantTable { TableName = "Bàn 01", Capacity = 2, Location = "Tầng trệt" },
                    new RestaurantTable { TableName = "Bàn 02", Capacity = 4, Location = "Tầng trệt" },
                    new RestaurantTable { TableName = "Bàn 03", Capacity = 4, Location = "Tầng trệt" },
                    new RestaurantTable { TableName = "Bàn 04", Capacity = 6, Location = "Tầng 1" },
                    new RestaurantTable { TableName = "Bàn 05", Capacity = 8, Location = "Tầng 1" },
                    new RestaurantTable { TableName = "Bàn VIP 01", Capacity = 10, Location = "Phòng VIP" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
