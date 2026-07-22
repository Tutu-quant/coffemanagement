namespace Quản_lý_quán_cafe.Models.Enums
{
    /// <summary>
    /// Các trạng thái thanh toán trong hệ thống
    /// </summary>
    public enum PaymentStatusEnum
    {
        /// <summary>Chờ thanh toán</summary>
        Pending = 0,

        /// <summary>Đang xử lý thanh toán</summary>
        Processing = 1,

        /// <summary>Thanh toán thành công</summary>
        Completed = 2,

        /// <summary>Thanh toán thất bại</summary>
        Failed = 3,

        /// <summary>Hoàn tiền</summary>
        Refunded = 4
    }

    /// <summary>
    /// Hằng số trạng thái thanh toán (dùng khi so sánh với Database string)
    /// </summary>
    public static class PaymentStatusConstants
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";
    }
}
