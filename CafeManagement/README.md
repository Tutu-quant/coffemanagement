# ☕ Cafe Management - Hệ thống quản lý quán cafe (ASP.NET Core MVC)

Dự án quản lý quán cafe viết bằng **ASP.NET Core 8 MVC + Entity Framework Core (SQLite) + ASP.NET Core Identity**.

## Tính năng chính

- **Đặt bàn / đặt ghế trực tuyến**: khách đăng ký tài khoản, chọn bàn, chọn giờ, nhập số lượng ghế. Hệ thống tự **tính tiền cọc** = số ghế × đơn giá cọc/ghế (mặc định 20.000đ/ghế, cấu hình trong `appsettings.json`).
- **Quản lý order & tính tiền hoá đơn**: nhân viên xác nhận đặt bàn, mở order cho bàn, thêm/xoá món, hệ thống tự tính tổng tiền (trừ tiền cọc đã đặt, áp dụng giảm giá), xuất hoá đơn để in.
- **Phân quyền 3 cấp** bằng ASP.NET Core Identity:
  - **Admin**: toàn quyền quản trị (danh mục, món ăn, bàn/ghế, người dùng & phân quyền, xem doanh thu).
  - **Staff (Nhân viên)**: xác nhận đặt bàn, quản lý order, tính tiền/thanh toán, xem lịch sử hoá đơn.
  - **Customer (Người dùng)**: đăng ký/đăng nhập, đặt bàn, xem thực đơn, xem/huỷ đặt bàn của mình.
- Trang quản trị (Area `Admin`): CRUD danh mục, món ăn, bàn/ghế; quản lý người dùng và gán vai trò (Admin/Staff/Customer); dashboard thống kê doanh thu theo ngày/tháng.

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Không cần cài SQL Server — dự án dùng **SQLite**, file database (`cafe.db`) tự tạo khi chạy lần đầu.

## Cách chạy dự án

```bash
cd CafeManagement
dotnet restore
dotnet run
```

Mở trình duyệt tại địa chỉ hiển thị trong console (mặc định `http://localhost:5080`).

> Lần chạy đầu tiên, ứng dụng sẽ tự động tạo database SQLite, tạo sẵn 3 vai trò (Admin, Staff, Customer), 1 tài khoản Admin, 1 tài khoản Staff, và một số danh mục/món/bàn mẫu.

## Tài khoản mẫu (đã seed sẵn)

| Vai trò | Email             | Mật khẩu   |
|---------|--------------------|------------|
| Admin   | admin@cafe.com     | Admin@123  |
| Staff   | staff@cafe.com     | Staff@123  |

Khách hàng tự đăng ký tài khoản mới tại trang **Đăng ký** — tài khoản mới sẽ tự động được gán vai trò **Customer**.

## Luồng sử dụng chính

1. **Khách hàng**: đăng ký/đăng nhập → vào **Đặt bàn** → chọn bàn, giờ, số ghế → xem tiền cọc ước tính → xác nhận đặt bàn → theo dõi trạng thái tại **Đặt bàn của tôi**.
2. **Nhân viên** (đăng nhập bằng tài khoản Staff): vào **Quản lý bàn / Order** → xác nhận các đặt bàn đang chờ → mở order cho bàn → thêm món khách gọi → bấm **Thanh toán** → nhập giảm giá (nếu có) và phương thức thanh toán → xác nhận → in hoá đơn.
3. **Admin**: vào menu **Quản trị** → quản lý danh mục/món/bàn, xem **Người dùng & phân quyền** để đổi vai trò cho từng tài khoản, xem **Tổng quan** để theo dõi doanh thu.

## Cấu trúc thư mục chính

```
CafeManagement/
├── Data/                  # DbContext + Seed dữ liệu mẫu
├── Models/                # Category, Product, RestaurantTable, Booking, Order, OrderDetail...
├── Controllers/           # Home, Menu, Booking, Order (dùng chung)
├── Areas/
│   ├── Admin/              # Khu vực quản trị (chỉ Admin truy cập)
│   │   ├── Controllers/     # Dashboard, Categories, Products, Tables, Users
│   │   └── Views/
│   └── Identity/            # Trang đăng ký tuỳ chỉnh (tự gán role Customer)
├── Views/                  # Razor views cho Home, Menu, Booking, Order
└── wwwroot/                 # CSS (Bootstrap 5 qua CDN)
```

## Cấu hình tiền cọc / ghế

Sửa trong `appsettings.json`:

```json
"AppSettings": {
  "DepositPerSeat": 20000
}
```

## Chuyển sang dùng EF Core Migrations (tuỳ chọn)

Mặc định dự án dùng `Database.EnsureCreated()` để đơn giản hoá (không cần cài `dotnet-ef`). Nếu muốn dùng Migrations chuyên nghiệp hơn (để có lịch sử thay đổi schema):

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

Sau đó trong `Data/DbInitializer.cs`, đổi dòng `EnsureCreatedAsync()` thành `MigrateAsync()`.

## Ghi chú

- Toàn bộ giao diện dùng tiếng Việt, Bootstrap 5 (tải qua CDN, cần kết nối internet khi chạy trên trình duyệt).
- Mật khẩu Identity được cấu hình đơn giản (tối thiểu 6 ký tự, không bắt buộc ký tự đặc biệt) — có thể chỉnh trong `Program.cs` nếu cần bảo mật cao hơn.
- Đây là dự án nền tảng (starter), bạn có thể mở rộng thêm: thanh toán online, gửi email xác nhận đặt bàn, in hoá đơn PDF, báo cáo doanh thu theo biểu đồ, v.v.
