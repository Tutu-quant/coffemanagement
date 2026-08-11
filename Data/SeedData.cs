using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository;

namespace Quản_lý_quán_cafe.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, bool seedDemoData)
        {
            try
            {

                await context.Database.EnsureCreatedAsync();
                await EnsureLoyaltyAndVoucherSchemaAsync(context);
                await EnsureUsersCustomerColumnAsync(context);
                await EnsureReservationsTimeColumnAsync(context);
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE IF NOT EXISTS PaymentAccountSettings (
                        PaymentAccountSettingID INTEGER NOT NULL CONSTRAINT PK_PaymentAccountSettings PRIMARY KEY AUTOINCREMENT,
                        Provider TEXT NOT NULL,
                        AccountNumber TEXT NOT NULL,
                        AccountName TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL,
                        UpdatedBy TEXT NULL
                    );
                    """);
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentAccountSettings_Provider
                    ON PaymentAccountSettings (Provider);
                    """);
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE IF NOT EXISTS PaymentGatewaySettings (
                        PaymentGatewaySettingID INTEGER NOT NULL CONSTRAINT PK_PaymentGatewaySettings PRIMARY KEY AUTOINCREMENT,
                        Provider TEXT NOT NULL,
                        MerchantId TEXT NOT NULL,
                        ApiKeyProtected TEXT NULL,
                        SecretKeyProtected TEXT NULL,
                        Endpoint TEXT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL,
                        UpdatedBy TEXT NULL
                    );
                    """);
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_PaymentGatewaySettings_Provider
                    ON PaymentGatewaySettings (Provider);
                    """);
                if (seedDemoData)
                {
                await context.Database.ExecuteSqlRawAsync("""
                    INSERT INTO PaymentAccountSettings
                        (Provider, AccountNumber, AccountName, IsActive, CreatedAt)
                    SELECT 'Placeholder', '19074356859019', 'QUAN CAFE THU NGHIEM', 1, CURRENT_TIMESTAMP
                    WHERE NOT EXISTS (
                        SELECT 1 FROM PaymentAccountSettings WHERE Provider = 'Placeholder'
                    );
                    """);
                await context.Database.ExecuteSqlRawAsync("""
                    UPDATE PaymentAccountSettings
                    SET AccountName = 'QUAN CAFE THU NGHIEM', UpdatedAt = CURRENT_TIMESTAMP
                    WHERE Provider = 'Placeholder'
                      AND AccountNumber = '19074356859019'
                      AND AccountName = 'TAI KHOAN QUAN';
                    """);
                await context.Database.ExecuteSqlRawAsync("""
                    INSERT INTO PaymentGatewaySettings
                        (Provider, MerchantId, Endpoint, IsActive, CreatedAt)
                    SELECT 'VietQR', '970407', 'https://img.vietqr.io/image', 1, CURRENT_TIMESTAMP
                    WHERE NOT EXISTS (
                        SELECT 1 FROM PaymentGatewaySettings WHERE Provider = 'VietQR'
                    );
                    """);

                }


                await SeedRolesAsync(context);
                await SeedVouchersAsync(context);

                if (seedDemoData)
                {
                // Seed Users (which also creates Employees)
                await SeedUsersAsync(context);

                // Seed Employees (if needed separately)
                await SeedEmployeesAsync(context);

                // Seed Categories
                await SeedCategoriesAsync(context);


                await SeedProductsAsync(context);
                await SeedDemoProductsAsync(context);


                await SeedCustomersAsync(context);
                await SeedDemoCustomerAccountsAsync(context);
                await SeedRestaurantTablesAsync(context);
                }

                await LinkCustomerUsersAsync(context);

                // Seed demo orders for revenue chart
                if (seedDemoData)
                {
                    await SeedDemoOrdersAsync(context);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seed database error: {ex.Message}");
                throw;
            }
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            var roles = new List<Role>
            {
                new Role
                {
                    RoleName = "Admin",
                    Description = "Quản trị viên hệ thống",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Role
                {
                    RoleName = "Cashier",
                    Description = "Thu ngân quán café",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Role
                {
                    RoleName = "Customer",
                    Description = "Khách hàng",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            var existingRoles = await context.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var existing = existingRoles.FirstOrDefault(item => item.RoleName == role.RoleName);
                if (existing is null)
                    context.Roles.Add(role);
                else if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.Description = role.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedVouchersAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;
            var requestedVouchers = new[]
            {
                new Voucher
                {
                    Code = "GIAMNUAGIA",
                    Name = "Giảm nửa giá",
                    DiscountType = Voucher.PercentDiscount,
                    DiscountValue = 50,
                    IsActive = true,
                    CreatedAt = now
                },
                new Voucher
                {
                    Code = "20PHANTRAM",
                    Name = "Giảm 20 phần trăm",
                    DiscountType = Voucher.PercentDiscount,
                    DiscountValue = 20,
                    IsActive = true,
                    CreatedAt = now
                },
                new Voucher
                {
                    Code = "20KBOTUI",
                    Name = "Giảm 20.000đ",
                    DiscountType = Voucher.FixedDiscount,
                    DiscountValue = 20_000,
                    IsActive = true,
                    CreatedAt = now
                },
                new Voucher
                {
                    Code = "50KBOTUI",
                    Name = "Giảm 50.000đ",
                    DiscountType = Voucher.FixedDiscount,
                    DiscountValue = 50_000,
                    IsActive = true,
                    CreatedAt = now
                },
                new Voucher
                {
                    Code = "1LITTINHYEU",
                    Name = "Giảm 100.000đ",
                    DiscountType = Voucher.FixedDiscount,
                    DiscountValue = 100_000,
                    IsActive = true,
                    CreatedAt = now
                },
                new Voucher
                {
                    Code = "DOCDACMIENPHI",
                    Name = "Độc đắc miễn phí",
                    DiscountType = Voucher.PercentDiscount,
                    DiscountValue = 100,
                    IsActive = true,
                    CreatedAt = now
                }
            };

            var existingVouchers = await context.Vouchers.ToListAsync();
            foreach (var requested in requestedVouchers)
            {
                var existing = existingVouchers.FirstOrDefault(voucher =>
                    string.Equals(voucher.Code, requested.Code, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    context.Vouchers.Add(requested);
                    continue;
                }

                existing.Code = requested.Code;
                existing.Name = requested.Name;
                existing.DiscountType = requested.DiscountType;
                existing.DiscountValue = requested.DiscountValue;
                existing.IsActive = true;
                existing.StartDate = null;
                existing.EndDate = null;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedEmployeesAsync(ApplicationDbContext context)
        {
            if (await context.Employees.AnyAsync())
                return;

            // Employees will be created with Users later
        }

        private static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin" && !r.IsDeleted);
            var cashierRole = await context.Roles.FirstAsync(r => r.RoleName == "Cashier" && !r.IsDeleted);
            var customerRole = await context.Roles.FirstAsync(r => r.RoleName == "Customer" && !r.IsDeleted);

            var hashedPassword = UserRepository.HashPassword("123456");

            // Create Admin User and Employee together
            var adminEmployee = new Employee
            {
                FullName = "Quản trị viên",
                Gender = "Male",
                Email = "admin@cafe.com",
                Phone = "0123456789",
                Address = "123 Main Street",
                Position = "Quản lý",
                Department = "Quản trị",
                HireDate = DateTime.UtcNow.AddYears(-1),
                Salary = 10000000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = hashedPassword,
                RoleID = adminRole.RoleID,
                Employee = adminEmployee,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Create Cashier User and Employee together
            var cashierEmployee = new Employee
            {
                FullName = "Thu ngân",
                Gender = "Female",
                Email = "cashier@cafe.com",
                Phone = "0123456790",
                Address = "123 Main Street",
                Position = "Thu ngân",
                Department = "Thu ngân",
                HireDate = DateTime.UtcNow.AddMonths(-6),
                Salary = 5000000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var cashierUser = new User
            {
                Username = "cashier",
                PasswordHash = hashedPassword,
                RoleID = cashierRole.RoleID,
                Employee = cashierEmployee,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Customer User (no Employee)
            var customerUser = new User
            {
                Username = "customer",
                PasswordHash = hashedPassword,
                RoleID = customerRole.RoleID,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var users = new List<User> { adminUser, cashierUser, customerUser };
            var existingUsernames = await context.Users.Select(user => user.Username).ToListAsync();
            await context.Users.AddRangeAsync(users.Where(user => !existingUsernames.Contains(user.Username)));
            await context.SaveChangesAsync();
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryName = "Cà Phê",
                    Description = "Các loại cà phê khác nhau",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Trà",
                    Description = "Các loại trà thơm ngon",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Bánh",
                    Description = "Các loại bánh mặn và bánh ngọt",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    CategoryName = "Khác",
                    Description = "Các sản phẩm khác",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            var existingNames = await context.Categories.Select(category => category.CategoryName).ToListAsync();
            await context.Categories.AddRangeAsync(categories.Where(category => !existingNames.Contains(category.CategoryName)));
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            var coffeeCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Cà Phê" && !c.IsDeleted);
            var teaCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Trà" && !c.IsDeleted);
            var cakeCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Bánh" && !c.IsDeleted);

            var products = new List<Product>
            {
                new Product
                {
                    ProductName = "Espresso",
                    Description = "Cà phê Espresso đậm đà",
                    CategoryID = coffeeCategory.CategoryID,
                    Price = 30000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Cappuccino",
                    Description = "Cà phê Cappuccino thơm ngon",
                    CategoryID = coffeeCategory.CategoryID,
                    Price = 40000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Latte",
                    Description = "Cà phê Latte kem sữa",
                    CategoryID = coffeeCategory.CategoryID,
                    Price = 45000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Trà Đen",
                    Description = "Trà đen thơm ngon",
                    CategoryID = teaCategory.CategoryID,
                    Price = 25000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Trà Xanh",
                    Description = "Trà xanh tươi mát",
                    CategoryID = teaCategory.CategoryID,
                    Price = 25000,
                    Quantity = 100,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Bánh Mì",
                    Description = "Bánh mì tươi ngon",
                    CategoryID = cakeCategory.CategoryID,
                    Price = 15000,
                    Quantity = 50,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Product
                {
                    ProductName = "Bánh Kem",
                    Description = "Bánh kem hoa quả",
                    CategoryID = cakeCategory.CategoryID,
                    Price = 50000,
                    Quantity = 20,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            var existingProductNames = await context.Products.Select(product => product.ProductName).ToListAsync();
            await context.Products.AddRangeAsync(products.Where(product => !existingProductNames.Contains(product.ProductName)));
            await context.SaveChangesAsync();
        }

        private static async Task SeedDemoProductsAsync(ApplicationDbContext context)
        {
            var coffeeCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Cà Phê");
            var teaCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Trà");
            var cakeCategory = await context.Categories.FirstAsync(c => c.CategoryName == "Bánh");
            var now = DateTime.UtcNow;
            var demoProducts = new[]
            {
                new Product { ProductName = "Cà phê sữa đá", Description = "Cà phê pha phin cùng sữa đặc", CategoryID = coffeeCategory.CategoryID, Price = 35000, Quantity = 100, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new Product { ProductName = "Bạc xỉu", Description = "Sữa thơm béo với một chút cà phê", CategoryID = coffeeCategory.CategoryID, Price = 40000, Quantity = 100, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new Product { ProductName = "Trà đào cam sả", Description = "Trà đào thanh mát cùng cam và sả", CategoryID = teaCategory.CategoryID, Price = 45000, Quantity = 100, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new Product { ProductName = "Matcha latte", Description = "Matcha và sữa tươi", CategoryID = teaCategory.CategoryID, Price = 49000, Quantity = 100, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new Product { ProductName = "Croissant bơ", Description = "Bánh sừng bò bơ nướng giòn", CategoryID = cakeCategory.CategoryID, Price = 32000, Quantity = 50, IsActive = true, CreatedAt = now, UpdatedAt = now }
            };

            var existingNames = await context.Products.Select(p => p.ProductName).ToListAsync();
            await context.Products.AddRangeAsync(demoProducts.Where(p => !existingNames.Contains(p.ProductName)));
            await context.SaveChangesAsync();
        }

        private static async Task SeedCustomersAsync(ApplicationDbContext context)
        {
            if (await context.Customers.AnyAsync())
                return;

            var customers = new List<Customer>
            {
                new Customer
                {
                    CustomerName = "Khách hàng Demo",
                    Phone = "0987654321",
                    Email = "demo@customer.com",
                    Address = "123 Customer Street",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDemoCustomerAccountsAsync(ApplicationDbContext context)
        {
            var customerRole = await context.Roles
                .FirstAsync(role => role.RoleName == "Customer" && !role.IsDeleted);
            var accounts = new[]
            {
                new DemoCustomerAccount("0900000001", "Khách tích điểm 01", "loyalty01@demo.brewpoint.local", 120),
                new DemoCustomerAccount("0900000002", "Khách tích điểm 02", "loyalty02@demo.brewpoint.local", 250),
                new DemoCustomerAccount("0900000003", "Khách tích điểm 03", "loyalty03@demo.brewpoint.local", 500),
                new DemoCustomerAccount("0900000004", "Khách tích điểm 04", "loyalty04@demo.brewpoint.local", 1_000)
            };

            foreach (var account in accounts)
            {
                var user = await context.Users
                    .Include(item => item.Customer)
                    .FirstOrDefaultAsync(item => item.Username == account.Phone);

                if (user is null)
                {
                    var customer = await context.Customers
                        .FirstOrDefaultAsync(item => item.Phone == account.Phone || item.Email == account.Email);
                    if (customer is null)
                    {
                        customer = new Customer
                        {
                            CustomerName = account.Name,
                            Phone = account.Phone,
                            Email = account.Email,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                    }
                    else if (await context.Users.AnyAsync(item => item.CustomerID == customer.CustomerID))
                    {
                        throw new InvalidOperationException(
                            $"Demo phone {account.Phone} is already linked to another user.");
                    }

                    user = new User
                    {
                        Username = account.Phone,
                        PasswordHash = UserRepository.HashPassword("123456"),
                        RoleID = customerRole.RoleID,
                        Customer = customer,
                        IsActive = true,
                        CreatedBy = "DemoSeed",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(user);
                    await context.SaveChangesAsync();
                }

                if (user.RoleID != customerRole.RoleID || user.Customer is null)
                {
                    throw new InvalidOperationException(
                        $"Demo username {account.Phone} exists but is not a linked customer account.");
                }

                var idempotencyKey = $"seed:demo-customer:{account.Phone}:initial-points";
                if (await context.PointHistories.AnyAsync(history => history.IdempotencyKey == idempotencyKey))
                    continue;

                user.Customer.RewardPoints += account.InitialPoints;
                user.Customer.UpdatedAt = DateTime.UtcNow;
                context.PointHistories.Add(new PointHistory
                {
                    CustomerID = user.Customer.CustomerID,
                    Points = account.InitialPoints,
                    BalanceAfter = user.Customer.RewardPoints,
                    TransactionType = "Grant",
                    Description = "Điểm khởi tạo cho tài khoản demo",
                    IdempotencyKey = idempotencyKey,
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureLoyaltyAndVoucherSchemaAsync(ApplicationDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            if (!await HasColumnAsync(connection, transaction, "Customers", "RewardPoints"))
            {
                await ExecuteSchemaCommandAsync(
                    connection,
                    transaction,
                    "ALTER TABLE \"Customers\" ADD COLUMN \"RewardPoints\" INTEGER NOT NULL DEFAULT 0;");
            }

            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Customers_Phone"
                ON "Customers" ("Phone")
                WHERE "Phone" IS NOT NULL;
                """);

            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE TABLE IF NOT EXISTS "Vouchers" (
                    "VoucherID" INTEGER NOT NULL CONSTRAINT "PK_Vouchers" PRIMARY KEY AUTOINCREMENT,
                    "Code" TEXT COLLATE NOCASE NOT NULL,
                    "Name" TEXT NOT NULL,
                    "DiscountType" TEXT NOT NULL,
                    "DiscountValue" TEXT NOT NULL,
                    "IsActive" INTEGER NOT NULL DEFAULT 1,
                    "StartDate" TEXT NULL,
                    "EndDate" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "UpdatedAt" TEXT NULL DEFAULT CURRENT_TIMESTAMP,
                    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                    CONSTRAINT "CK_Vouchers_Discount" CHECK (
                        ("DiscountType" = 'Percent' AND CAST("DiscountValue" AS NUMERIC) > 0
                            AND CAST("DiscountValue" AS NUMERIC) <= 100)
                        OR ("DiscountType" = 'Fixed' AND CAST("DiscountValue" AS NUMERIC) > 0)
                    )
                );
                """);
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Vouchers_Code\" ON \"Vouchers\" (\"Code\");");

            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "SubtotalAmount",
                "ALTER TABLE \"Orders\" ADD COLUMN \"SubtotalAmount\" TEXT NOT NULL DEFAULT '0.0';");
            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "VoucherDiscountAmount",
                "ALTER TABLE \"Orders\" ADD COLUMN \"VoucherDiscountAmount\" TEXT NOT NULL DEFAULT '0.0';");
            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "PointDiscountAmount",
                "ALTER TABLE \"Orders\" ADD COLUMN \"PointDiscountAmount\" TEXT NOT NULL DEFAULT '0.0';");
            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "VoucherID",
                "ALTER TABLE \"Orders\" ADD COLUMN \"VoucherID\" INTEGER NULL REFERENCES \"Vouchers\" (\"VoucherID\") ON DELETE RESTRICT;");
            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "VoucherCode",
                "ALTER TABLE \"Orders\" ADD COLUMN \"VoucherCode\" TEXT NULL;");
            await EnsureColumnAsync(
                connection,
                transaction,
                "Orders",
                "IsLoyaltyCustomerAssigned",
                "ALTER TABLE \"Orders\" ADD COLUMN \"IsLoyaltyCustomerAssigned\" INTEGER NOT NULL DEFAULT 0;");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_Orders_VoucherID\" ON \"Orders\" (\"VoucherID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                UPDATE "Orders"
                SET "SubtotalAmount" = COALESCE(
                    (SELECT SUM(CAST("Subtotal" AS NUMERIC))
                     FROM "OrderDetails"
                     WHERE "OrderDetails"."OrderID" = "Orders"."OrderID"
                       AND "OrderDetails"."IsDeleted" = 0),
                    CAST("TotalAmount" AS NUMERIC),
                    0)
                WHERE CAST("SubtotalAmount" AS NUMERIC) = 0;
                """);

            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE TABLE IF NOT EXISTS "PointHistories" (
                    "PointHistoryID" INTEGER NOT NULL CONSTRAINT "PK_PointHistories" PRIMARY KEY AUTOINCREMENT,
                    "CustomerID" INTEGER NOT NULL,
                    "Points" INTEGER NOT NULL,
                    "BalanceAfter" INTEGER NOT NULL DEFAULT 0,
                    "TransactionType" TEXT NOT NULL,
                    "Description" TEXT NULL,
                    "OrderID" INTEGER NULL,
                    "ActorUserID" INTEGER NULL,
                    "IdempotencyKey" TEXT NULL,
                    "TransactionDate" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    "UpdatedAt" TEXT NULL DEFAULT CURRENT_TIMESTAMP,
                    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                    CONSTRAINT "FK_PointHistories_Customers_CustomerID"
                        FOREIGN KEY ("CustomerID") REFERENCES "Customers" ("CustomerID") ON DELETE RESTRICT,
                    CONSTRAINT "FK_PointHistories_Orders_OrderID"
                        FOREIGN KEY ("OrderID") REFERENCES "Orders" ("OrderID") ON DELETE SET NULL,
                    CONSTRAINT "FK_PointHistories_Users_ActorUserID"
                        FOREIGN KEY ("ActorUserID") REFERENCES "Users" ("UserID") ON DELETE SET NULL,
                    CONSTRAINT "CK_PointHistories_Points_NotZero" CHECK ("Points" <> 0),
                    CONSTRAINT "CK_PointHistories_BalanceAfter_NonNegative" CHECK ("BalanceAfter" >= 0)
                );
                """);

            if (!await HasColumnAsync(connection, transaction, "PointHistories", "BalanceAfter"))
            {
                await ExecuteSchemaCommandAsync(
                    connection,
                    transaction,
                    "ALTER TABLE \"PointHistories\" ADD COLUMN \"BalanceAfter\" INTEGER NOT NULL DEFAULT 0;");
                await ExecuteSchemaCommandAsync(
                    connection,
                    transaction,
                    """
                    UPDATE "PointHistories" AS current
                    SET "BalanceAfter" = MAX(0,
                        COALESCE((
                            SELECT "RewardPoints"
                            FROM "Customers"
                            WHERE "CustomerID" = current."CustomerID"
                        ), 0)
                        - COALESCE((
                            SELECT SUM(later."Points")
                            FROM "PointHistories" AS later
                            WHERE later."CustomerID" = current."CustomerID"
                              AND (
                                  later."TransactionDate" > current."TransactionDate"
                                  OR (later."TransactionDate" = current."TransactionDate"
                                      AND later."PointHistoryID" > current."PointHistoryID")
                              )
                        ), 0));
                    """);
            }
            await EnsureColumnAsync(
                connection,
                transaction,
                "PointHistories",
                "ActorUserID",
                "ALTER TABLE \"PointHistories\" ADD COLUMN \"ActorUserID\" INTEGER NULL REFERENCES \"Users\" (\"UserID\") ON DELETE SET NULL;");
            await EnsureColumnAsync(
                connection,
                transaction,
                "PointHistories",
                "IdempotencyKey",
                "ALTER TABLE \"PointHistories\" ADD COLUMN \"IdempotencyKey\" TEXT NULL;");

            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_PointHistories_CustomerID\" ON \"PointHistories\" (\"CustomerID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_PointHistories_OrderID\" ON \"PointHistories\" (\"OrderID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_PointHistories_ActorUserID\" ON \"PointHistories\" (\"ActorUserID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PointHistories_IdempotencyKey"
                ON "PointHistories" ("IdempotencyKey")
                WHERE "IdempotencyKey" IS NOT NULL;
                """);

            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE TABLE IF NOT EXISTS "OrderPointRedemptions" (
                    "OrderPointRedemptionID" INTEGER NOT NULL
                        CONSTRAINT "PK_OrderPointRedemptions" PRIMARY KEY AUTOINCREMENT,
                    "OrderID" INTEGER NOT NULL,
                    "CustomerID" INTEGER NOT NULL,
                    "PointHistoryID" INTEGER NULL,
                    "PointsUsed" INTEGER NOT NULL,
                    "DiscountAmount" TEXT NOT NULL,
                    "Sequence" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT "FK_OrderPointRedemptions_Orders_OrderID"
                        FOREIGN KEY ("OrderID") REFERENCES "Orders" ("OrderID") ON DELETE RESTRICT,
                    CONSTRAINT "FK_OrderPointRedemptions_Customers_CustomerID"
                        FOREIGN KEY ("CustomerID") REFERENCES "Customers" ("CustomerID") ON DELETE RESTRICT,
                    CONSTRAINT "FK_OrderPointRedemptions_PointHistories_PointHistoryID"
                        FOREIGN KEY ("PointHistoryID") REFERENCES "PointHistories" ("PointHistoryID") ON DELETE RESTRICT,
                    CONSTRAINT "CK_OrderPointRedemptions_Points_Positive" CHECK ("PointsUsed" > 0),
                    CONSTRAINT "CK_OrderPointRedemptions_Discount_Positive"
                        CHECK (CAST("DiscountAmount" AS NUMERIC) > 0),
                    CONSTRAINT "CK_OrderPointRedemptions_Sequence_NonNegative" CHECK ("Sequence" >= 0)
                );
                """);
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_OrderPointRedemptions_OrderID_CustomerID\" ON \"OrderPointRedemptions\" (\"OrderID\", \"CustomerID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_OrderPointRedemptions_CustomerID\" ON \"OrderPointRedemptions\" (\"CustomerID\");");
            await ExecuteSchemaCommandAsync(
                connection,
                transaction,
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrderPointRedemptions_PointHistoryID"
                ON "OrderPointRedemptions" ("PointHistoryID")
                WHERE "PointHistoryID" IS NOT NULL;
                """);

            await transaction.CommitAsync();
        }

        private static async Task EnsureColumnAsync(
            System.Data.Common.DbConnection connection,
            System.Data.Common.DbTransaction transaction,
            string tableName,
            string columnName,
            string addColumnCommand)
        {
            if (!await HasColumnAsync(connection, transaction, tableName, columnName))
                await ExecuteSchemaCommandAsync(connection, transaction, addColumnCommand);
        }

        private static async Task<bool> HasColumnAsync(
            System.Data.Common.DbConnection connection,
            System.Data.Common.DbTransaction transaction,
            string tableName,
            string columnName)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static async Task ExecuteSchemaCommandAsync(
            System.Data.Common.DbConnection connection,
            System.Data.Common.DbTransaction transaction,
            string commandText)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        private static async Task EnsureUsersCustomerColumnAsync(ApplicationDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            var hasColumn = false;
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Users);";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (string.Equals(reader.GetString(1), "CustomerID", StringComparison.OrdinalIgnoreCase))
                    {
                        hasColumn = true;
                        break;
                    }
                }
            }

            if (!hasColumn)
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE Users ADD COLUMN CustomerID INTEGER NULL REFERENCES Customers(CustomerID) ON DELETE SET NULL;");
            await context.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_CustomerID ON Users(CustomerID) WHERE CustomerID IS NOT NULL;");
        }

        private static async Task EnsureReservationsTimeColumnAsync(ApplicationDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            var hasColumn = false;
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Reservations);";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (string.Equals(reader.GetString(1), "ReservationTime", StringComparison.OrdinalIgnoreCase))
                    {
                        hasColumn = true;
                        break;
                    }
                }
            }

            if (!hasColumn)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE Reservations ADD COLUMN ReservationTime TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';");
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE Reservations SET ReservationTime = ReservationDate;");
            }
        }

        private static async Task LinkCustomerUsersAsync(ApplicationDbContext context)
        {
            var customerRoleId = await context.Roles
                .Where(role => role.RoleName == "Customer" && !role.IsDeleted)
                .Select(role => (int?)role.RoleID)
                .FirstOrDefaultAsync();
            if (!customerRoleId.HasValue) return;

            var users = await context.Users
                .Where(user => user.RoleID == customerRoleId.Value && !user.IsDeleted && user.CustomerID == null)
                .ToListAsync();
            foreach (var user in users)
            {
                var identityEmail = $"{user.Username}@local.cafe";
                var customer = await context.Customers.FirstOrDefaultAsync(item =>
                    item.Email != null && item.Email.ToLower() == identityEmail.ToLower());
                if (customer is null)
                {
                    customer = new Customer
                    {
                        CustomerName = user.Username,
                        Email = identityEmail,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Customers.Add(customer);
                    await context.SaveChangesAsync();
                }
                user.CustomerID = customer.CustomerID;
                if (customer.IsDeleted || !customer.IsActive)
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedRestaurantTablesAsync(ApplicationDbContext context)
        {
            var tables = new List<RestaurantTable>();

            for (int i = 1; i <= 10; i++)
            {
                int capacity = (i % 3 == 0) ? 6 : (i % 2 == 0) ? 4 : 2;
                tables.Add(new RestaurantTable
                {
                    TableNumber = $"T{i:D2}",
                    Capacity = capacity,
                    TableStatus = "Available",
                    Location = i <= 5 ? "Tầng 1" : "Tầng 2",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            var existingTableNumbers = await context.RestaurantTables.Select(table => table.TableNumber).ToListAsync();
            await context.RestaurantTables.AddRangeAsync(tables.Where(table => !existingTableNumbers.Contains(table.TableNumber)));
            await context.SaveChangesAsync();
        }

        private sealed record DemoCustomerAccount(
            string Phone,
            string Name,
            string Email,
            int InitialPoints);

        private static async Task SeedDemoOrdersAsync(ApplicationDbContext context)
        {
            // Kiểm tra nếu đã có Orders trong tháng này
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var existingOrders = await context.Orders
                .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear)
                .CountAsync();

            // Nếu đã có >= 20 Orders thì không tạo thêm
            if (existingOrders >= 20)
                return;

            var products = await context.Products.ToListAsync();
            if (!products.Any())
                return;

            var tables = await context.RestaurantTables.ToListAsync();
            if (!tables.Any())
                return;

            var today = DateTime.UtcNow;
            var ordersToAdd = new List<Order>();
            var random = new Random(42); // Fixed seed for consistency

            // Tạo Orders cho mỗi ngày trong tháng
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            for (int day = 1; day <= Math.Min(daysInMonth, today.Day); day++)
            {
                // Tạo 2-4 Orders cho mỗi ngày
                int ordersPerDay = random.Next(2, 5);

                for (int i = 0; i < ordersPerDay; i++)
                {
                    var hour = random.Next(8, 20); // Orders từ 8AM đến 8PM
                    var minute = random.Next(0, 60);
                    var orderDate = new DateTime(today.Year, today.Month, day, hour, minute, 0, DateTimeKind.Utc);

                    // Skip nếu ngày đó lớn hơn hôm nay
                    if (orderDate > today)
                        continue;

                    decimal totalAmount = 0;
                    var orderDetails = new List<OrderDetail>();

                    // Mỗi order có 1-4 items
                    int itemCount = random.Next(1, 5);
                    for (int j = 0; j < itemCount; j++)
                    {
                        var product = products[random.Next(products.Count)];
                        int quantity = random.Next(1, 4);
                        decimal unitPrice = product.Price;
                        decimal subtotal = unitPrice * quantity;

                        orderDetails.Add(new OrderDetail
                        {
                            ProductID = product.ProductID,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Subtotal = subtotal,
                            Notes = string.Empty,
                            IsDeleted = false,
                            CreatedAt = orderDate,
                            UpdatedAt = orderDate
                        });

                        totalAmount += subtotal;
                    }

                    var table = tables[random.Next(tables.Count)];
                    var order = new Order
                    {
                        TableID = table.TableID,
                        OrderDate = orderDate,
                        OrderStatus = "Completed",
                        TotalAmount = totalAmount,
                        SubtotalAmount = totalAmount,
                        VoucherDiscountAmount = 0,
                        PointDiscountAmount = 0,
                        Notes = string.Empty,
                        IsDeleted = false,
                        CreatedAt = orderDate,
                        UpdatedAt = orderDate,
                        CompletedDate = orderDate.AddMinutes(random.Next(15, 45)),
                        OrderDetails = orderDetails
                    };

                    ordersToAdd.Add(order);
                }
            }

            // Chỉ thêm nếu không quá 100 Orders mới
            if (ordersToAdd.Count <= 100 && ordersToAdd.Count > 0)
            {
                await context.Orders.AddRangeAsync(ordersToAdd);
                await context.SaveChangesAsync();
            }
        }

    }
}
