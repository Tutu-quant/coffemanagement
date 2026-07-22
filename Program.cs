using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Extensions;
using Quản_lý_quán_cafe.Middleware;
using Quản_lý_quán_cafe.Repository;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "BrewPoint.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();

// Register Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRestaurantTableService, RestaurantTableService>();

// Add logging
builder.Logging.AddConsole();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    // Show detailed error page in Development
    app.UseDeveloperExceptionPage();
}
else
{
    // In Production use custom JSON exception middleware and HSTS
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHsts();
    // HTTPS redirect in production only
    app.UseHttpsRedirection();
}

// Static files BEFORE routing and HTTPS redirect in development
app.UseStaticFiles();
app.UseRouting();

// Add Session Middleware
app.UseSession();

// Logging Middleware (always)
app.UseMiddleware<LoggingMiddleware>();

// Apply migrations and seed data
try
{
    await app.MigrateDatabaseAsync();
    await app.SeedDatabaseAsync();
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
}

// DEBUG: Endpoint để manual seed data (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/seed", async () =>
    {
        try
        {
            await app.SeedDatabaseAsync();
            return "Seed data completed!";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    });
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
