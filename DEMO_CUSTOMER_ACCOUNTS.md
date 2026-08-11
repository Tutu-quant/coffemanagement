# Tài khoản khách hàng demo

Các tài khoản dưới đây chỉ được tạo khi `SeedData:EnableDemoData` bật. Tên đăng nhập trùng với số điện thoại để có thể dùng cùng một giá trị khi đăng nhập và khi thu ngân tìm tài khoản.

Mật khẩu mặc định cho tất cả tài khoản: `123456`.

| Tên đăng nhập / SĐT | Tên hiển thị | Email demo | Điểm ban đầu |
|---|---|---|---:|
| `0900000001` | Khách tích điểm 01 | `loyalty01@demo.brewpoint.local` | 120 |
| `0900000002` | Khách tích điểm 02 | `loyalty02@demo.brewpoint.local` | 250 |
| `0900000003` | Khách tích điểm 03 | `loyalty03@demo.brewpoint.local` | 500 |
| `0900000004` | Khách tích điểm 04 | `loyalty04@demo.brewpoint.local` | 1.000 |

Các số điện thoại và email trên là dữ liệu giả dành riêng cho kiểm thử. Seed chạy lặp lại không tạo trùng tài khoản, không đặt lại mật khẩu đã đổi và không tặng lại điểm ban đầu sau khi điểm đã được sử dụng.

Không bật `SeedData:EnableDemoData` trong môi trường production.
