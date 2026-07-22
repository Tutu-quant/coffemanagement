using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        // DbSets
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PointHistory> PointHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== ROLE ====================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleID);

                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Users)
                    .WithOne(u => u.Role)
                    .HasForeignKey(u => u.RoleID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== EMPLOYEE ====================
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeID);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Gender)
                    .HasMaxLength(10);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.Address)
                    .HasMaxLength(500);

                entity.Property(e => e.Salary)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Users)
                    .WithOne(u => u.Employee)
                    .HasForeignKey(u => u.EmployeeID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== USER ====================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(100);

                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Employee)
                    .WithMany(emp => emp.Users)
                    .HasForeignKey(e => e.EmployeeID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== CATEGORY ====================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryID);

                entity.Property(e => e.CategoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Products)
                    .WithOne(p => p.Category)
                    .HasForeignKey(p => p.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== PRODUCT ====================
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductID);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.OrderDetails)
                    .WithOne(od => od.Product)
                    .HasForeignKey(od => od.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Reviews)
                    .WithOne(r => r.Product)
                    .HasForeignKey(r => r.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Promotions)
                    .WithOne(p => p.Product)
                    .HasForeignKey(p => p.ProductID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== CUSTOMER ====================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.Address)
                    .HasMaxLength(500);

                entity.Property(e => e.RewardPoints)
                    .HasDefaultValue(0);

                entity.Property(e => e.TotalSpent)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);

                entity.Property(e => e.MembershipTier)
                    .HasMaxLength(30)
                    .HasDefaultValue("Member");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Reservations)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PointHistories)
                    .WithOne(ph => ph.Customer)
                    .HasForeignKey(ph => ph.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Reviews)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== RESTAURANT TABLE ====================
            modelBuilder.Entity<RestaurantTable>(entity =>
            {
                entity.HasKey(e => e.TableID);

                entity.Property(e => e.TableNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.TableNumber)
                    .IsUnique();

                entity.Property(e => e.TableStatus)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Location)
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Orders)
                    .WithOne(o => o.Table)
                    .HasForeignKey(o => o.TableID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Reservations)
                    .WithOne(r => r.Table)
                    .HasForeignKey(r => r.TableID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== ORDER ====================
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderID);

                entity.Property(e => e.OrderStatus)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Table)
                    .WithMany(t => t.Orders)
                    .HasForeignKey(e => e.TableID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Payment)
                    .WithOne(p => p.Order)
                    .HasForeignKey<Payment>(p => p.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.OrderDetails)
                    .WithOne(od => od.Order)
                    .HasForeignKey(od => od.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== ORDER DETAIL ====================
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(e => e.OrderDetailID);

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(e => e.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderDetails)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== RESERVATION ====================
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(e => e.ReservationID);

                entity.Property(e => e.ReservationStatus)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Reservations)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Table)
                    .WithMany(t => t.Reservations)
                    .HasForeignKey(e => e.TableID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== PAYMENT ====================
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentID);

                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TransactionCode)
                    .HasMaxLength(100);

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Order)
                    .WithOne(o => o.Payment)
                    .HasForeignKey<Payment>(e => e.OrderID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PromotionUses)
                    .WithOne(p => p.Payment)
                    .HasForeignKey(p => p.PaymentID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== PROMOTION ====================
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(e => e.PromotionID);

                entity.Property(e => e.PromotionName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                entity.Property(e => e.DiscountPercentage)
                    .HasPrecision(5, 2);

                entity.Property(e => e.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Promotions)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Payment)
                    .WithMany(p => p.PromotionUses)
                    .HasForeignKey(e => e.PaymentID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== REVIEW ====================
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewID);

                entity.Property(e => e.Comment)
                    .HasMaxLength(1000);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Reviews)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== POINT HISTORY ====================
            modelBuilder.Entity<PointHistory>(entity =>
            {
                entity.HasKey(e => e.PointHistoryID);

                entity.Property(e => e.TransactionType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.PointHistories)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderID)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
