# Backend của Dương Nguyên Thượng

Các phần đã tích hợp vào bản `manhhung1011/Qu-n-l-qu-n-cafe`:

- `GET /api/products?categoryId=&search=`: danh sách và tìm kiếm sản phẩm.
- `GET /api/tables/available?at=&guests=`: tìm bàn phù hợp, loại trừ bàn bảo trì và lịch trùng trong 2 giờ.
- `GET /api/reports/revenue?from=&to=`: doanh thu thực nhận và món bán chạy (Admin).
- `/Customer/Reservations/Create`: đặt bàn, kiểm tra sức chứa và lịch trùng.
- `/Customer/Reservations`: lịch sử và hủy đặt bàn.
- `/Cashier/Tables`: Cashier/Admin cập nhật trạng thái vận hành của bàn.
- `/Cashier/POS`: tạo order, thêm/sửa/xóa món, cập nhật tồn kho và thanh toán.

Trạng thái bàn dùng thống nhất: `Available`, `Reserved`, `Occupied`, `WaitingPayment`, `Maintenance`.
Khi không có bàn ngoài trạng thái `Maintenance`, giao diện Customer hiển thị **“Quán chưa hỗ trợ đặt bàn”** thay cho form.
