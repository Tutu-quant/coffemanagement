# BrewPoint – Quản lý quán cafe

Ứng dụng ASP.NET Core MVC trên .NET 10 dành cho quản lý quán cafe.

## Chức năng

- Admin quản lý danh mục, sản phẩm, khách hàng, tài khoản và bàn.
- Cashier quản lý tình trạng bàn, order, tồn kho và thanh toán.
- Customer đặt bàn, xem lịch sử, dùng điểm thưởng hoặc voucher và hủy đặt bàn.
- Hóa đơn hỗ trợ nhiều tài khoản cùng góp điểm, voucher server-side và VietQR theo giá sau khấu trừ.
- Kiểm tra sức chứa, bàn bảo trì và lịch đặt trùng trong khoảng hai giờ.
- API sản phẩm, bàn khả dụng và báo cáo doanh thu.

Khi quán không có bàn khả dụng, trang đặt bàn hiển thị “Quán chưa hỗ trợ đặt bàn”.

## Chạy dự án

Yêu cầu [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
cd "D:\qlqcf2"
dotnet restore
dotnet run
```

Ứng dụng sử dụng SQLite. File `brewpoint.db` được tạo tự động và không được đưa vào Git.

## Tài khoản phát triển

Các tài khoản dưới đây chỉ được seed khi chạy môi trường `Development` (hoặc khi chủ động bật `SeedData:EnableDemoData`). Môi trường Production không tạo tài khoản demo. Không dùng các mật khẩu này khi triển khai thật.

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Admin | `admin` | `123456` |
| Cashier | `cashier` | `123456` |
| Customer | `customer` | `123456` |

Danh sách tài khoản khách hàng dùng SĐT giả làm tên đăng nhập nằm trong [`DEMO_CUSTOMER_ACCOUNTS.md`](DEMO_CUSTOMER_ACCOUNTS.md).

Chi tiết endpoint xem tại `BACKEND_API.md`.

## Cấu trúc

Project đã được hợp nhất tại thư mục gốc. File solution và project:

- `Quản lý quán cafe.slnx`
- `Quản lý quán cafe.csproj`

Dashboard quản trị khả dụng tại `/Admin/Dashboard` sau khi đăng nhập bằng tài khoản Admin.
