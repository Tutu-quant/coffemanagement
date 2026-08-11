using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Services;

/// <summary>
/// Development-only service to seed demo data for testing and demonstration purposes.
/// All demo data is marked with a "🔧 DEMO:" prefix in Notes field for easy identification and removal.
/// </summary>
public class DemoDataSeeder
{
    private const string DEMO_MARKER = "🔧 DEMO:";
    private const string DEMO_EMAIL = "demo@brewpoint.local";
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(ApplicationDbContext context, ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds demo data (idempotent - clears old demo orders/reservations first).
    /// Creates demo orders with different priority levels and demo reservations.
    /// </summary>
    public async Task<DemoDataSeedResult> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // First, clear any existing demo orders/reservations (idempotent)
            var clearResult = await ClearDemoDataAsync(cancellationToken);
            _logger.LogInformation("Cleared existing demo data: {Result}", clearResult.Message);

            var result = new DemoDataSeedResult();
            var now = DateTime.UtcNow;
            var businessNow = BusinessClock.Now;

            _logger.LogInformation("Starting demo data seeding...");

            // Get available tables and products
            var tables = await _context.RestaurantTables
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.TableID)
                .Take(10)
                .ToListAsync(cancellationToken);

            var products = await _context.Products
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.ProductID)
                .Take(5)
                .ToListAsync(cancellationToken);

            if (tables.Count < 4)
            {
                _logger.LogWarning("Not enough tables for demo data. Found: {TableCount}", tables.Count);
                result.ErrorMessage = "Cần ít nhất 4 bàn để seeding demo data";
                return result;
            }

            if (products.Count < 3)
            {
                _logger.LogWarning("Not enough products for demo data. Found: {ProductCount}", products.Count);
                result.ErrorMessage = "Cần ít nhất 3 sản phẩm để seeding demo data";
                return result;
            }

            // Find or create demo customer (idempotent - prefer email marker, fall back to any customer
            // that already has demo orders/reservations attached)
            var demoCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == DEMO_EMAIL
                    || c.CustomerName == "DEMO Customer"
                    || c.Orders.Any(o => o.Notes != null && o.Notes.Contains(DEMO_MARKER))
                    || c.Reservations.Any(r => r.Notes != null && r.Notes.Contains(DEMO_MARKER)),
                    cancellationToken);

            if (demoCustomer == null)
            {
                demoCustomer = new Customer
                {
                    CustomerName = "DEMO Customer",
                    Phone = null,  // Leave phone null to avoid unique constraint collisions
                    Email = DEMO_EMAIL,
                    IsActive = true,
                    CreatedAt = now,
                    IsDeleted = false
                };
                _context.Customers.Add(demoCustomer);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created new demo customer with ID {CustomerID}", demoCustomer.CustomerID);
            }
            else
            {
                // Reuse existing customer. Do not overwrite potentially real data. Ensure it's marked active.
                demoCustomer.IsActive = true;
                if (demoCustomer.CreatedAt == default)
                    demoCustomer.CreatedAt = now;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Reusing existing demo customer with ID {CustomerID}", demoCustomer.CustomerID);
            }

            // ===== DEMO ORDERS =====

            // Order 1: Pending, created 3 minutes ago (Normal priority)
            var order1 = new Order
            {
                TableID = tables[0].TableID,
                OrderDate = now.AddMinutes(-3),
                CreatedAt = now.AddMinutes(-3),
                UpdatedAt = now.AddMinutes(-3),
                OrderStatus = "Pending",
                Notes = $"{DEMO_MARKER} Order 1: Normal (3 min)",
                SubtotalAmount = products[0].Price * 2,
                VoucherDiscountAmount = 0,
                PointDiscountAmount = 0,
                TotalAmount = products[0].Price * 2,
                IsDeleted = false
            };

            // Order 2: Pending, created 12 minutes ago (High priority)
            var subtotal2 = (products[1].Price * 1) + (products[2].Price * 2);
            var order2 = new Order
            {
                TableID = tables[1].TableID,
                OrderDate = now.AddMinutes(-12),
                CreatedAt = now.AddMinutes(-12),
                UpdatedAt = now.AddMinutes(-12),
                OrderStatus = "Pending",
                Notes = $"{DEMO_MARKER} Order 2: High Priority (12 min)",
                SubtotalAmount = subtotal2,
                VoucherDiscountAmount = 0,
                PointDiscountAmount = 0,
                TotalAmount = subtotal2,
                IsDeleted = false
            };

            // Order 3: Preparing, created 17 minutes ago (Urgent - yellow warning)
            var subtotal3 = products[0].Price * 1;
            var order3 = new Order
            {
                TableID = tables[2].TableID,
                OrderDate = now.AddMinutes(-17),
                CreatedAt = now.AddMinutes(-17),
                UpdatedAt = now.AddMinutes(-17),
                OrderStatus = "Preparing",
                Notes = $"{DEMO_MARKER} Order 3: Urgent (17 min)",
                SubtotalAmount = subtotal3,
                VoucherDiscountAmount = 0,
                PointDiscountAmount = 0,
                TotalAmount = subtotal3,
                IsDeleted = false
            };

            // Order 4: Preparing, created 23 minutes ago (Critical - red, over limit)
            var subtotal4 = products[1].Price * 3;
            var order4 = new Order
            {
                TableID = tables[3].TableID,
                OrderDate = now.AddMinutes(-23),
                CreatedAt = now.AddMinutes(-23),
                UpdatedAt = now.AddMinutes(-23),
                OrderStatus = "Preparing",
                Notes = $"{DEMO_MARKER} Order 4: Critical (23 min)",
                SubtotalAmount = subtotal4,
                VoucherDiscountAmount = 0,
                PointDiscountAmount = 0,
                TotalAmount = subtotal4,
                IsDeleted = false
            };

            // Add orders first (without OrderDetails)
            _context.Orders.AddRange(order1, order2, order3, order4);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Orders saved successfully. Order IDs: {Order1}, {Order2}, {Order3}, {Order4}",
                    order1.OrderID, order2.OrderID, order3.OrderID, order4.OrderID);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException saving orders: {Message}\nInner: {InnerMessage}",
                    ex.Message, ex.InnerException?.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving orders: {Message}\nStackTrace: {StackTrace}",
                    ex.InnerException?.Message ?? ex.Message, ex.StackTrace);
                throw;
            }

            // Now add OrderDetails with the generated OrderIDs
            var orderDetail1 = new OrderDetail
            {
                OrderID = order1.OrderID,
                ProductID = products[0].ProductID,
                Quantity = 2,
                UnitPrice = products[0].Price,
                Subtotal = products[0].Price * 2,
                IsDeleted = false,
                CreatedAt = now.AddMinutes(-3)
            };

            var orderDetail2_1 = new OrderDetail
            {
                OrderID = order2.OrderID,
                ProductID = products[1].ProductID,
                Quantity = 1,
                UnitPrice = products[1].Price,
                Subtotal = products[1].Price,
                IsDeleted = false,
                CreatedAt = now.AddMinutes(-12)
            };

            var orderDetail2_2 = new OrderDetail
            {
                OrderID = order2.OrderID,
                ProductID = products[2].ProductID,
                Quantity = 2,
                UnitPrice = products[2].Price,
                Subtotal = products[2].Price * 2,
                IsDeleted = false,
                CreatedAt = now.AddMinutes(-12)
            };

            var orderDetail3 = new OrderDetail
            {
                OrderID = order3.OrderID,
                ProductID = products[0].ProductID,
                Quantity = 1,
                UnitPrice = products[0].Price,
                Subtotal = products[0].Price,
                IsDeleted = false,
                CreatedAt = now.AddMinutes(-17)
            };

            var orderDetail4 = new OrderDetail
            {
                OrderID = order4.OrderID,
                ProductID = products[1].ProductID,
                Quantity = 3,
                UnitPrice = products[1].Price,
                Subtotal = products[1].Price * 3,
                IsDeleted = false,
                CreatedAt = now.AddMinutes(-23)
            };

            _context.OrderDetails.AddRange(orderDetail1, orderDetail2_1, orderDetail2_2, orderDetail3, orderDetail4);
            await _context.SaveChangesAsync(cancellationToken);
            result.OrdersCreated = 4;

            _logger.LogInformation("Created 4 demo orders");

            // ===== DEMO RESERVATIONS =====

            // Reservation 1: 5 minutes ago (should be within overdue range, status Pending)
            var res1 = new Reservation
            {
                TableID = tables[4].TableID,
                CustomerID = demoCustomer.CustomerID,
                ReservationDate = businessNow.AddMinutes(-5),
                ReservationTime = businessNow.AddMinutes(-5),
                NumberOfGuests = 4,
                ReservationStatus = "Pending",
                Notes = $"{DEMO_MARKER} Res 1: Overdue (5 min)",
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Reservation 2: 20 minutes ago (should trigger auto-cancel or be very overdue)
            var res2 = new Reservation
            {
                TableID = tables[5].TableID,
                CustomerID = demoCustomer.CustomerID,
                ReservationDate = businessNow.AddMinutes(-20),
                ReservationTime = businessNow.AddMinutes(-20),
                NumberOfGuests = 2,
                ReservationStatus = "Pending",
                Notes = $"{DEMO_MARKER} Res 2: Very Overdue (20 min)",
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Reservation 3: 10 minutes in future (should show as upcoming)
            var res3 = new Reservation
            {
                TableID = tables[6].TableID,
                CustomerID = demoCustomer.CustomerID,
                ReservationDate = businessNow.AddMinutes(10),
                ReservationTime = businessNow.AddMinutes(10),
                NumberOfGuests = 6,
                ReservationStatus = "Confirmed",
                Notes = $"{DEMO_MARKER} Res 3: Upcoming (10 min)",
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Reservations.AddRange(res1, res2, res3);
            await _context.SaveChangesAsync(cancellationToken);
            result.ReservationsCreated = 3;

            _logger.LogInformation("Created 3 demo reservations");

            result.Success = true;
            result.Message = $"Demo data seeded successfully: {result.OrdersCreated} orders, {result.ReservationsCreated} reservations";

            _logger.LogInformation(result.Message);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo data");
            return new DemoDataSeedResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Clears all demo data (marked with "🔧 DEMO:" prefix in Notes field).
    /// Only removes records explicitly marked as demo data. Demo customers are only removed
    /// if they are not referenced by any remaining orders or reservations.
    /// </summary>
    public async Task<DemoClearResult> ClearDemoDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new DemoClearResult();

            _logger.LogInformation("Clearing demo data...");

            // Clear demo order details (find via their orders)
            var demoOrderIds = await _context.Orders
                .Where(o => o.Notes != null && o.Notes.Contains(DEMO_MARKER))
                .Select(o => o.OrderID)
                .ToListAsync(cancellationToken);

            var demoOrderDetails = await _context.OrderDetails
                .Where(od => demoOrderIds.Contains(od.OrderID))
                .ToListAsync(cancellationToken);
            if (demoOrderDetails.Count > 0)
            {
                _context.OrderDetails.RemoveRange(demoOrderDetails);
                await _context.SaveChangesAsync(cancellationToken);
            }
            result.OrderDetailsDeleted = demoOrderDetails.Count;

            // Clear demo orders
            var demoOrders = await _context.Orders
                .Where(o => o.Notes != null && o.Notes.Contains(DEMO_MARKER))
                .ToListAsync(cancellationToken);
            if (demoOrders.Count > 0)
            {
                _context.Orders.RemoveRange(demoOrders);
                await _context.SaveChangesAsync(cancellationToken);
            }
            result.OrdersDeleted = demoOrders.Count;

            // Clear demo reservations
            var demoReservations = await _context.Reservations
                .Where(r => r.Notes != null && r.Notes.Contains(DEMO_MARKER))
                .ToListAsync(cancellationToken);
            if (demoReservations.Count > 0)
            {
                _context.Reservations.RemoveRange(demoReservations);
                await _context.SaveChangesAsync(cancellationToken);
            }
            result.ReservationsDeleted = demoReservations.Count;

            // Find candidate demo customers by email or name marker
            var demoCustomers = await _context.Customers
                .Where(c => c.Email == DEMO_EMAIL || c.CustomerName == "DEMO Customer")
                .ToListAsync(cancellationToken);

            var deletedCustomers = 0;
            foreach (var cust in demoCustomers)
            {
                var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerID == cust.CustomerID, cancellationToken);
                var hasReservations = await _context.Reservations.AnyAsync(r => r.CustomerID == cust.CustomerID, cancellationToken);
                if (!hasOrders && !hasReservations)
                {
                    _context.Customers.Remove(cust);
                    deletedCustomers++;
                }
                else
                {
                    _logger.LogInformation("Skipping deletion of demo customer {CustomerID} because it has existing references.", cust.CustomerID);
                }
            }

            if (deletedCustomers > 0)
                await _context.SaveChangesAsync(cancellationToken);

            result.CustomersDeleted = deletedCustomers;

            result.Success = true;
            result.Message = $"Demo data cleared: {result.OrdersDeleted} orders, " +
                           $"{result.ReservationsDeleted} reservations, " +
                           $"{result.CustomersDeleted} customers";

            _logger.LogInformation(result.Message);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing demo data");
            return new DemoClearResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Result from demo data seeding operation.
/// </summary>
public class DemoDataSeedResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int OrdersCreated { get; set; }
    public int ReservationsCreated { get; set; }
}

/// <summary>
/// Result from demo data clearing operation.
/// </summary>
public class DemoClearResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int OrdersDeleted { get; set; }
    public int ReservationsDeleted { get; set; }
    public int CustomersDeleted { get; set; }
    public int OrderDetailsDeleted { get; set; }
}
