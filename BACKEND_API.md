# Backend của Dương Nguyên Thượng

Các phần đã tích hợp vào bản `manhhung1011/Qu-n-l-qu-n-cafe`:

- `GET /api/products?categoryId=&search=`: danh sách và tìm kiếm sản phẩm.
- `GET /api/tables/available?at=&guests=`: tìm bàn phù hợp, loại trừ bàn bảo trì và lịch trùng trong 2 giờ.
- `GET /api/reports/revenue?from=&to=`: doanh thu thực nhận và món bán chạy (Admin).
- `/Customer/Reservations/Create`: đặt bàn, kiểm tra sức chứa và lịch trùng.
- `/Customer/Reservations`: lịch sử và hủy đặt bàn.
- `/Cashier/Tables`: Cashier/Admin cập nhật trạng thái vận hành của bàn.
- `/Cashier/POS`: tạo order, thêm/sửa/xóa món, cập nhật tồn kho và thanh toán.
- `/Cashier/Dashboard`: doanh thu thực nhận trong ngày, danh sách hóa đơn chưa thanh toán và lối tắt sang POS để tính tiền.
- `POST /api/payments/qr/intents`: tạo payment intent và Quick Link VietQR cho đơn hàng (Cashier/Admin).
- `GET /api/payments/qr/status/{orderId}`: kiểm tra trạng thái thanh toán QR (Cashier/Admin).
- `POST /api/payments/qr/webhook`: callback xác nhận thanh toán, yêu cầu header `X-Webhook-Secret` và body `{ orderId, amount, transactionCode }`.

## Cấu hình MoMo

Admin cập nhật số tài khoản và tên người nhận tại **Admin Dashboard > Tài khoản nhận tiền**. Khóa merchant phải được cấu hình bằng environment variable: `QrPayment__PartnerCode`, `QrPayment__AccessKey`, `QrPayment__SecretKey`, `QrPayment__RedirectUrl`, `QrPayment__IpnUrl` và `QrPayment__WebhookSecret`. Không commit secret thật vào Git.

Trạng thái bàn dùng thống nhất: `Available`, `Reserved`, `Occupied`, `WaitingPayment`, `Maintenance`.
Khi không có bàn ngoài trạng thái `Maintenance`, giao diện Customer hiển thị **“Quán chưa hỗ trợ đặt bàn”** thay cho form.
