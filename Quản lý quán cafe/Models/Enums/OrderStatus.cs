namespace Quản_lý_quán_cafe.Models.Enums
{
    /// <summary>
    /// Các trạng thái đơn hàng trong hệ thống
    /// </summary>
    public enum OrderStatusEnum
    {
        /// <summary>Chờ xác nhận từ nhân viên</summary>
        Pending = 0,

        /// <summary>Đang pha chế/chuẩn bị đơn hàng</summary>
        Preparing = 1,

        /// <summary>Sẵn sàng phục vụ/Đơn đã hoàn thành</summary>
        Ready = 2,

        /// <summary>Chờ thanh toán</summary>
        WaitingPayment = 3,

        /// <summary>Đã thanh toán/Hoàn thành</summary>
        Completed = 4,

        /// <summary>Đã hủy</summary>
        Cancelled = 5
    }

    /// <summary>
    /// Hằng số trạng thái đơn hàng (dùng khi so sánh với Database string)
    /// </summary>
    public static class OrderStatusConstants
    {
        public const string Pending = "Pending";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string WaitingPayment = "WaitingPayment";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
