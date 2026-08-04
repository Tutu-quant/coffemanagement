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
                await SeedData.InitializeAsync(context);
            }
        }

    }
}
