namespace Quản_lý_quán_cafe.Models.Enums
{


    public enum PaymentStatusEnum
    {

        Pending = 0,


        Processing = 1,


        Completed = 2,


        Failed = 3,


        Refunded = 4
    }


    public static class PaymentStatusConstants
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";
    }
}
