namespace CafeManagement.Models
{
    public enum TableStatus
    {
        Trong = 0,          // Bàn trống, có thể đặt
        DaDat = 1,          // Đã được đặt trước
        DangPhucVu = 2,     // Đang có khách ngồi / order mở
        BaoTri = 3          // Ngừng phục vụ / bảo trì
    }

    public enum BookingStatus
    {
        ChoXacNhan = 0,     // Khách vừa đặt, chờ nhân viên xác nhận
        DaXacNhan = 1,      // Nhân viên đã xác nhận
        DaHuy = 2,          // Bị huỷ (khách huỷ hoặc quá giờ)
        HoanThanh = 3       // Đã phục vụ xong / đã thanh toán
    }

    public enum OrderStatus
    {
        DangMo = 0,         // Order đang mở, có thể thêm món
        DaThanhToan = 1,    // Đã tính tiền / thanh toán
        DaHuy = 2           // Huỷ order
    }
}
