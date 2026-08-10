using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Extensions;
using Quản_lý_quán_cafe.Middleware;
using Quản_lý_quán_cafe.Repository;
using Quản_lý_quán_cafe.Repository.Implementations;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Realtime;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.FormFieldName = "__RequestVerificationToken";
});

builder.Services.AddDataProtection();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "BrewPoint.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();

// Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRestaurantTableService, RestaurantTableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<CustomerSessionService>();

// Payment
builder.Services.AddSingleton<PaymentGatewaySecretProtector>();
builder.Services.AddSingleton<Quản_lý_quán_cafe.Realtime.IRealtimeUpdateNotifier,
    Quản_lý_quán_cafe.Realtime.RealtimeUpdateNotifier>();

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Ensure ReservationTime column exists in SQLite DB (for older schemas)
try
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connString) && connString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
    {
        using var conn = new SqliteConnection(connString);
        conn.Open();
        // Check if Reservations table has ReservationTime column
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA table_info('Reservations');";
        using var reader = cmd.ExecuteReader();
        var hasReservationTime = false;
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, "ReservationTime", StringComparison.OrdinalIgnoreCase))
            {
                hasReservationTime = true;
                break;
            }
        }

        if (!hasReservationTime)
        {
            // Add column and populate from ReservationDate if possible
            using var addCmd = conn.CreateCommand();
            addCmd.CommandText = "ALTER TABLE Reservations ADD COLUMN ReservationTime TEXT;";
            addCmd.ExecuteNonQuery();

            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE Reservations SET ReservationTime = ReservationDate;";
            updateCmd.ExecuteNonQuery();
        }

        conn.Close();
    }
}
catch
{
    // Ignore errors here; dashboard fallback will handle missing column at runtime
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHsts();
}

// Add cache-busting headers for static files
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/css") ||
        context.Request.Path.StartsWithSegments("/js") ||
        context.Request.Path.StartsWithSegments("/lib"))
    {
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseMiddleware<LoggingMiddleware>();

await app.SeedDatabaseAsync();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<AppStateHub>("/hubs/app-state");

await app.RunAsync();
