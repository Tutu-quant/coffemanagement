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
- `GET /api/loyalty/accounts?query=`: Cashier/Admin tìm tài khoản theo tên đăng nhập, tên khách hoặc SĐT.
- `GET /api/orders/{orderId}/discounts`: đọc báo giá ưu đãi hiện hành.
- `POST /api/orders/{orderId}/discounts/points`: áp dụng toàn bộ điểm của một hoặc nhiều tài khoản theo thứ tự gửi lên.
- `POST /api/orders/{orderId}/discounts/voucher`: áp dụng một voucher; điểm và voucher không cộng dồn.
- `DELETE /api/orders/{orderId}/discounts`: bỏ ưu đãi khỏi đơn chưa thanh toán.
- Placeholder QR không có webhook và không tự động xác nhận thanh toán.

## Điểm thưởng và voucher

- Mỗi `10.000đ` tạm tính trước khấu trừ tạo `1` điểm khi thanh toán hoàn tất; `1` điểm giảm `100đ`.
- Khi điểm vượt giá trị đơn, hệ thống chỉ trừ số điểm nguyên đủ để giảm; phần tiền lẻ dưới `100đ` vẫn phải thanh toán và điểm chưa dùng vẫn thuộc tài khoản cũ. Nhiều tài khoản không thể chuyển điểm cho nhau nhưng có thể cùng góp điểm cho một đơn tại POS.
- Với đơn POS chưa gắn khách, tài khoản đầu tiên cashier chọn là tài khoản nhận điểm mới. Checkout vẫn gắn và cộng điểm cho tài khoản này ngay cả khi khách không đổi điểm/không dùng voucher; chủ sở hữu của đơn Customer hoặc đặt bàn không thể bị ghi đè.
- QR luôn lấy tổng tiền sau ưu đãi từ server. Đơn còn `0đ` không tạo QR, được hoàn tất với phương thức `Discount` rồi chuyển sang in hóa đơn.
- Voucher được seed idempotent: `GIAMNUAGIA` (50%), `20PHANTRAM` (20%), `20KBOTUI` (20.000đ), `50KBOTUI` (50.000đ), `1LITTINHYEU` (100.000đ), `DOCDACMIENPHI` (100%). Phần giảm vượt quá tạm tính bị bỏ và tổng tiền không âm.
- Admin tặng điểm từ chi tiết khách hàng; mọi lần cộng, trừ và tặng đều ghi vào `PointHistories` với số dư sau giao dịch.

## Placeholder thanh toán QR

Admin cập nhật tài khoản và tên người nhận tại **Admin Dashboard > Tài khoản nhận tiền**. Tính năng này chỉ là placeholder cho cổng thanh toán sẽ được chọn sau này.

Admin cấu hình VietQR tại **Admin Dashboard > Cấu hình VietQR**. Đây là luồng QR chuyển khoản có xác nhận thủ công; hệ thống không giả lập webhook hoặc trạng thái thanh toán tự động.

Riêng dữ liệu demo trong môi trường Development dùng tài khoản `19074356859019` và Techcombank (`BANK_ID = 970407`). Production không seed tài khoản/gateway thanh toán; Admin phải cấu hình người nhận trước khi dùng. Quick Link VietQR tự điền `amount` bằng tổng tiền sau giảm giá và `addInfo` bằng `BP{OrderID}`. Với VietQR, `PaymentGateway.MerchantId` được dùng là `BANK_ID` (mã BIN hoặc tên viết tắt ngân hàng).

Trạng thái bàn dùng thống nhất: `Available`, `Reserved`, `Occupied`, `WaitingPayment`, `Maintenance`.
Khi không có bàn ngoài trạng thái `Maintenance`, giao diện Customer hiển thị **“Quán chưa hỗ trợ đặt bàn”** thay cho form.
