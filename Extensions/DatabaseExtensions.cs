using Quản_lý_quán_cafe.Data;
using Microsoft.EntityFrameworkCore;

namespace Quản_lý_quán_cafe.Extensions
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Seed database with initial data
        /// </summary>
        public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await SeedData.InitializeAsync(context);
            }
        }

        /// <summary>
        /// Apply pending migrations and create database if not exists
        /// </summary>
        public static async Task MigrateDatabaseAsync(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
            }
        }
    }
}
