# BrewPoint – Quản lý quán cafe

Ứng dụng ASP.NET Core MVC trên .NET 10 dành cho quản lý quán cafe.

## Chức năng

- Admin quản lý danh mục, sản phẩm, khách hàng, tài khoản và bàn.
- Cashier quản lý tình trạng bàn, order, tồn kho và thanh toán.
- Customer đặt bàn, xem lịch sử và hủy đặt bàn.
- Kiểm tra sức chứa, bàn bảo trì và lịch đặt trùng trong khoảng hai giờ.
- API sản phẩm, bàn khả dụng và báo cáo doanh thu.

Khi quán không có bàn khả dụng, trang đặt bàn hiển thị “Quán chưa hỗ trợ đặt bàn”.

## Chạy dự án

Yêu cầu [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
cd "Quản lý quán cafe"
dotnet restore
dotnet run
```

Ứng dụng sử dụng SQLite. File `brewpoint.db` được tạo tự động và không được đưa vào Git.

## Tài khoản mẫu

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Admin | `admin` | `123456` |
| Cashier | `cashier` | `123456` |
| Customer | `customer` | `123456` |

Chi tiết endpoint xem tại `Quản lý quán cafe/BACKEND_API.md`.
