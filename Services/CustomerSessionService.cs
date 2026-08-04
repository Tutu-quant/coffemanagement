using Microsoft.AspNetCore.Http;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Services;

public class CustomerSessionService(ICustomerRepository customerRepository, IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICustomerRepository _customerRepository = customerRepository;

    /// <summary>
    /// Lấy hoặc tạo mới khách hàng dựa trên session hiện tại
    /// </summary>
    public async Task<Customer> GetOrCreateCustomerAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("HttpContext not available");

        var username = httpContext.Session.GetString("Username") ?? "customer";
        var email = $"{username}@local.cafe";

        var customer = await _customerRepository.GetByEmailAsync(email);

        if (customer is not null && !customer.IsDeleted)
            return customer;

        customer = new Customer
        {
            CustomerName = httpContext.Session.GetString("FullName") ?? username,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        return customer;
    }
}
