# Sơ đồ class BrewPoint

Tài liệu này phản ánh cấu trúc hiện tại của dự án ASP.NET Core MVC. GitHub có thể render trực tiếp các khối Mermaid bên dưới.

## 1. Domain model và quan hệ cơ sở dữ liệu

Ký hiệu `?` biểu thị thuộc tính có thể null. Quan hệ và lực lượng số được lấy từ `ApplicationDbContext.OnModelCreating`.

```mermaid
classDiagram
direction LR

class Role {
  +int RoleID
  +string RoleName
  +string? Description
  +bool IsDeleted
}

class User {
  +int UserID
  +string Username
  +string PasswordHash
  +int RoleID
  +int? EmployeeID
  +bool IsActive
  +DateTime? LastLogin
  +bool IsDeleted
}

class Employee {
  +int EmployeeID
  +string FullName
  +string Gender
  +DateTime? BirthDate
  +string? Phone
  +string? Email
  +DateTime? HireDate
  +decimal? Salary
  +bool IsDeleted
}

class Customer {
  +int CustomerID
  +string CustomerName
  +string? Phone
  +string? Email
  +int RewardPoints
  +decimal TotalSpent
  +string MembershipTier
  +bool IsActive
  +bool IsDeleted
}

class Category {
  +int CategoryID
  +string CategoryName
  +string? Description
  +bool IsActive
  +bool IsDeleted
}

class Product {
  +int ProductID
  +string ProductName
  +int CategoryID
  +decimal Price
  +int Quantity
  +string? ImageUrl
  +bool IsActive
  +bool IsDeleted
}

class RestaurantTable {
  +int TableID
  +string TableNumber
  +int Capacity
  +string TableStatus
  +string? Location
  +bool IsDeleted
}

class Reservation {
  +int ReservationID
  +int CustomerID
  +int TableID
  +DateTime ReservationDate
  +DateTime? CheckinTime
  +DateTime? CheckoutTime
  +int NumberOfGuests
  +string ReservationStatus
  +string? Notes
  +bool IsDeleted
}

class Order {
  +int OrderID
  +int? CustomerID
  +int? EmployeeID
  +int? TableID
  +decimal TotalAmount
  +string OrderStatus
  +DateTime OrderDate
  +DateTime? CompletedDate
  +int? PaymentID
  +string? Notes
  +bool IsDeleted
}

class OrderDetail {
  +int OrderDetailID
  +int OrderID
  +int ProductID
  +int Quantity
  +decimal UnitPrice
  +decimal Subtotal
  +string? Notes
  +bool IsDeleted
}

class Payment {
  +int PaymentID
  +int OrderID
  +decimal Amount
  +string PaymentMethod
  +string PaymentStatus
  +DateTime PaymentDate
  +string? TransactionCode
  +bool IsDeleted
}

class Promotion {
  +int PromotionID
  +int? ProductID
  +int? PaymentID
  +string PromotionName
  +decimal DiscountPercentage
  +decimal? DiscountAmount
  +DateTime StartDate
  +DateTime? EndDate
  +bool IsActive
}

class Review {
  +int ReviewID
  +int ProductID
  +int CustomerID
  +int Rating
  +string? Comment
  +DateTime ReviewDate
  +bool IsApproved
}

class PointHistory {
  +int PointHistoryID
  +int CustomerID
  +int Points
  +string TransactionType
  +int? OrderID
  +DateTime TransactionDate
}

class PaymentAccountSetting {
  +int PaymentAccountSettingID
  +string Provider
  +string AccountNumber
  +string AccountName
  +bool IsActive
  +string? UpdatedBy
}

class PaymentGatewaySetting {
  +int PaymentGatewaySettingID
  +string Provider
  +string MerchantId
  +string? ApiKeyProtected
  +string? SecretKeyProtected
  +string? Endpoint
  +bool IsActive
  +string? UpdatedBy
}

Role "1" --> "0..*" User : phân quyền
Employee "0..1" --> "0..*" User : tài khoản nhân viên
Employee "0..1" --> "0..*" Order : xử lý
Customer "0..1" --> "0..*" Order : đặt món
Customer "1" --> "0..*" Reservation : đặt bàn
Customer "1" --> "0..*" Review : đánh giá
Customer "1" --> "0..*" PointHistory : tích điểm
Category "1" --> "0..*" Product : phân loại
Product "1" --> "0..*" OrderDetail : được gọi
Product "1" --> "0..*" Review : nhận đánh giá
Product "0..1" --> "0..*" Promotion : áp dụng
RestaurantTable "0..1" --> "0..*" Order : phục vụ
RestaurantTable "1" --> "0..*" Reservation : được đặt
Order "1" *-- "1..*" OrderDetail : gồm
Order "1" *-- "0..1" Payment : thanh toán
Order "0..1" --> "0..*" PointHistory : phát sinh điểm
Payment "0..1" --> "0..*" Promotion : sử dụng
```

`PaymentAccountSetting` và `PaymentGatewaySetting` là cấu hình độc lập do admin quản lý. Chúng được API QR đọc khi tạo thông tin thanh toán, không có khóa ngoại tới `Payment`.

## 2. Kiến trúc lớp ứng dụng

Các controller cũ sử dụng mô hình Service/Repository; một số controller mới và các API thao tác trực tiếp qua `ApplicationDbContext`.

```mermaid
classDiagram
direction TB

class Controller {
  <<ASP.NET Core MVC>>
}
class ControllerBase {
  <<ASP.NET Core API>>
}
class ApplicationDbContext {
  <<EF Core DbContext>>
  +DbSet Entities
  +OnModelCreating()
}

class AdminControllers {
  +DashboardController
  +ProductsController
  +CategoriesController
  +CustomersController
  +OrdersController
  +RestaurantTablesController
  +EmployeesController
  +ReservationsController
}
class CashierControllers {
  +DashboardController
  +POSController
  +OrdersController
  +PaymentsController
  +TablesController
}
class CustomerControllers {
  +OrdersController
  +ReservationsController
}
class ApiControllers {
  +ProductsApiController
  +TablesApiController
  +ReportsApiController
  +QrPaymentsApiController
}
class AccountController

Controller <|-- AdminControllers
Controller <|-- CashierControllers
Controller <|-- CustomerControllers
Controller <|-- AccountController
ControllerBase <|-- ApiControllers

class IProductService { <<interface>> }
class ICategoryService { <<interface>> }
class ICustomerService { <<interface>> }
class IOrderService { <<interface>> }
class IRestaurantTableService { <<interface>> }
class IUserService { <<interface>> }
class IAccountService { <<interface>> }

class ProductService
class CategoryService
class CustomerService
class OrderService
class RestaurantTableService
class UserService
class AccountService

IProductService <|.. ProductService
ICategoryService <|.. CategoryService
ICustomerService <|.. CustomerService
IOrderService <|.. OrderService
IRestaurantTableService <|.. RestaurantTableService
IUserService <|.. UserService
IAccountService <|.. AccountService

AdminControllers ..> IProductService
AdminControllers ..> ICategoryService
AdminControllers ..> ICustomerService
AdminControllers ..> IOrderService
AdminControllers ..> IRestaurantTableService
CashierControllers ..> IOrderService
AccountController ..> IAccountService

class IProductRepository { <<interface>> }
class ICategoryRepository { <<interface>> }
class ICustomerRepository { <<interface>> }
class IOrderRepository { <<interface>> }
class IRestaurantTableRepository { <<interface>> }
class IUserRepository { <<interface>> }
class IReservationRepository { <<interface>> }

class ProductRepository
class CategoryRepository
class CustomerRepository
class OrderRepository
class RestaurantTableRepository
class UserRepository
class ReservationRepository

IProductRepository <|.. ProductRepository
ICategoryRepository <|.. CategoryRepository
ICustomerRepository <|.. CustomerRepository
IOrderRepository <|.. OrderRepository
IRestaurantTableRepository <|.. RestaurantTableRepository
IUserRepository <|.. UserRepository
IReservationRepository <|.. ReservationRepository

ProductService ..> IProductRepository
ProductService ..> ICategoryRepository
CategoryService ..> ICategoryRepository
CustomerService ..> ICustomerRepository
OrderService ..> IOrderRepository
RestaurantTableService ..> IRestaurantTableRepository
UserService ..> IUserRepository
AccountService ..> IUserRepository

ProductRepository ..> ApplicationDbContext
CategoryRepository ..> ApplicationDbContext
CustomerRepository ..> ApplicationDbContext
OrderRepository ..> ApplicationDbContext
RestaurantTableRepository ..> ApplicationDbContext
UserRepository ..> ApplicationDbContext
ReservationRepository ..> ApplicationDbContext

AdminControllers ..> ApplicationDbContext : dashboard, employee, reservation
CashierControllers ..> ApplicationDbContext : POS, payment, table
CustomerControllers ..> ApplicationDbContext
ApiControllers ..> ApplicationDbContext
```

## 3. Các ViewModel chính

```mermaid
classDiagram
direction LR

class LoginViewModel
class ProductListViewModel
class ProductCreateViewModel
class ProductEditViewModel
class CategoryViewModel
class CustomerViewModel
class RestaurantTableViewModel
class OrderListViewModel
class OrderDetailViewModel
class OrderItemViewModel
class OrderMenuViewModel
class ReservationViewModel
class EmployeeFormViewModel
class DashboardViewModel
class POSViewModel
class CashierDashboardViewModel

OrderDetailViewModel *-- "0..*" OrderItemViewModel
OrderMenuViewModel *-- "0..*" MenuCategoryViewModel
MenuCategoryViewModel *-- "0..*" MenuProductViewModel
POSViewModel *-- "0..*" POSProductViewModel
POSViewModel *-- "0..*" POSTableViewModel
POSViewModel *-- "0..*" POSOrderItemViewModel
DashboardViewModel *-- "0..*" PaymentGatewayViewModel
DashboardViewModel *-- "0..*" PaymentAccountViewModel
```

## Ghi chú thiết kế

- Hầu hết entity dùng xóa mềm qua `IsDeleted`.
- Trạng thái đơn, thanh toán và bàn hiện được lưu bằng `string`; dự án có các lớp hằng số/enum hỗ trợ nhưng chưa thống nhất hoàn toàn.
- `Order.PaymentID` tồn tại trong model, còn quan hệ một-một thực tế được EF Core ánh xạ bằng `Payment.OrderID`.
- `Customer` chưa liên kết trực tiếp với `User`; customer đăng nhập được ánh xạ theo email do controller tạo từ username.
