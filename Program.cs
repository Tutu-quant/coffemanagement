using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Extensions;
using Quản_lý_quán_cafe.Middleware;
using Quản_lý_quán_cafe.Repository;
using Quản_lý_quán_cafe.Repository.Implementations;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRestaurantTableService, RestaurantTableService>();
builder.Services.AddSingleton<PaymentGatewaySecretProtector>();
builder.Services.AddSingleton<Quản_lý_quán_cafe.Realtime.IRealtimeUpdateNotifier,
    Quản_lý_quán_cafe.Realtime.RealtimeUpdateNotifier>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseHsts();
}

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

app.MapHub<Quản_lý_quán_cafe.Realtime.AppStateHub>("/hubs/app-state");

await app.RunAsync();
