using CafeManagement.Repositories;
using CafeManagement.Repositories.Interfaces;
using CafeManagement.Services;
using CafeManagement.Services.Interfaces;
using CafeManagement.Mapping;

namespace CafeManagement.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register Repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<ITableRepository, TableRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Register Services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
