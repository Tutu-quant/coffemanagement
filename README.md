# ☕ Hệ Thống Quản Lý Quán Cafe

Hệ thống Quản lý Quán Cafe là một website được phát triển bằng **ASP.NET Core MVC (.NET 8)** nhằm hỗ trợ quản lý hoạt động của quán cà phê như quản lý sản phẩm, danh mục, bàn, đơn hàng, thanh toán, khách hàng và nhân viên.

Dự án được xây dựng với mục tiêu mô phỏng quy trình vận hành thực tế của một quán cafe, đồng thời áp dụng mô hình phát triển phần mềm hiện đại, dễ mở rộng và bảo trì.

---

## 🚀 Công nghệ sử dụng

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core (Code First)
- SQL Server
- Bootstrap 5
- HTML5 / CSS3 / JavaScript
- LINQ
- Repository Pattern
- ViewModel Pattern

---

## ✨ Chức năng chính

### Quản trị (Admin)

- Đăng nhập hệ thống
- Dashboard thống kê
- Quản lý danh mục
- Quản lý sản phẩm
- Quản lý bàn
- Quản lý đơn hàng
- Quản lý thanh toán
- Quản lý khách hàng
- Quản lý nhân viên
- Theo dõi doanh thu
- Phân quyền người dùng

### Thu ngân (Cashier)

- Giao diện POS
- Chọn bàn
- Thêm món vào hóa đơn
- Cập nhật số lượng
- Áp dụng giảm giá
- Thanh toán hóa đơn
- In hóa đơn

---

## 📂 Cấu trúc dự án

```
Areas
 ├── Admin
 ├── Cashier

Controllers
Models
ViewModels
Repositories
Services
Data
Migrations
wwwroot
```

---

## 🛠️ Hướng dẫn cài đặt

### 1. Clone dự án

```bash
git clone https://github.com/manhhung1011/Qu-n-l-qu-n-cafe.git
```

### 2. Mở bằng Visual Studio 2022/2026

Mở file Solution (`.sln`).

### 3. Cấu hình chuỗi kết nối

Mở file:

```
appsettings.json
```

Ví dụ:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=QLCafe;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 4. Cập nhật cơ sở dữ liệu

Mở Package Manager Console:

```powershell
Update-Database
```

Hoặc nếu chưa có Migration:

```powershell
Add-Migration InitialCreate
Update-Database
```

### 5. Chạy dự án

Nhấn **F5** hoặc **Ctrl + F5**.

---

## 🔑 Tài khoản demo

### Quản trị viên

| Tài khoản | Mật khẩu |
|-----------|----------|
| admin | 123456 |

### Thu ngân

| Tài khoản | Mật khẩu |
|-----------|----------|
| cashier | 123456 |

> **Lưu ý:** Thay đổi thông tin đăng nhập theo dữ liệu trong cơ sở dữ liệu nếu bạn đã chỉnh sửa.

---

## 📸 Một số giao diện

- Trang đăng nhập
  <img width="1917" height="1030" alt="image" src="https://github.com/user-attachments/assets/6f70cffe-5f32-4f84-8afa-e5a1960ca19e" />

- Dashboard
- Quản lý sản phẩm
- <img width="1917" height="1031" alt="image" src="https://github.com/user-attachments/assets/2a4e068f-1dc5-4187-a6f7-1a6d3c8f6548" />

- Giao diện POS
- <img width="1917" height="1031" alt="image" src="https://github.com/user-attachments/assets/27fd82fb-b704-4359-b4ce-6b4d1082e422" />

- Thanh toán
- Quản lý đơn hàng
- <img width="1917" height="1032" alt="image" src="https://github.com/user-attachments/assets/be2f6819-cb42-451d-8792-0a15541e1112" />




---

## 📌 Định hướng phát triển

- Quản lý kho nguyên liệu
- Quản lý khuyến mãi
- Tích điểm khách hàng
- Báo cáo doanh thu theo biểu đồ
- Quản lý nhiều chi nhánh
- Thanh toán QR Code
- Xuất hóa đơn PDF
- Responsive trên thiết bị di động

---

