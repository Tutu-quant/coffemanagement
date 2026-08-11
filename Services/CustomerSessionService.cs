using Microsoft.AspNetCore.Http;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Services;

public class CustomerSessionService(
    ICustomerRepository customerRepository,
    IUserRepository userRepository,
    IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// Lấy hoặc tạo mới khách hàng dựa trên session hiện tại
    /// </summary>
    public async Task<Customer> GetOrCreateCustomerAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("HttpContext not available");

        var customerId = httpContext.Session.GetInt32("CustomerId");
        if (customerId.HasValue)
        {
            var linkedCustomer = await _customerRepository.GetByIdAsync(customerId.Value);
            if (linkedCustomer is not null && !linkedCustomer.IsDeleted && linkedCustomer.IsActive)
                return linkedCustomer;
        }

        var userId = httpContext.Session.GetInt32("UserId");
        var user = userId.HasValue ? await _userRepository.GetByIdAsync(userId.Value) : null;
        if (user?.CustomerID is not null)
        {
            if (user.Customer is null || user.Customer.IsDeleted || !user.Customer.IsActive)
                throw new InvalidOperationException("Tài khoản khách hàng đã bị vô hiệu hóa.");
            httpContext.Session.SetInt32("CustomerId", user.Customer.CustomerID);
            return user.Customer;
        }

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
        if (user is not null)
        {
            user.CustomerID = customer.CustomerID;
            await _userRepository.UpdateAsync(user);
            httpContext.Session.SetInt32("CustomerId", customer.CustomerID);
        }
        return customer;
    }
}
