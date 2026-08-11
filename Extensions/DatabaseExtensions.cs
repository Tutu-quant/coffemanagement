using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Extensions
{
    public static class DatabaseExtensions
    {


        public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var seedDemoData = configuration.GetValue<bool?>("SeedData:EnableDemoData")
                    ?? environment.IsDevelopment();
                await SeedData.InitializeAsync(context, seedDemoData);
            }
        }

    }
}
