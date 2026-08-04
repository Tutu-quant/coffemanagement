namespace Quản_lý_quán_cafe.Models.Enums
{


    public enum OrderStatusEnum
    {

        Pending = 0,


        Preparing = 1,


        Ready = 2,


        WaitingPayment = 3,


        Completed = 4,


        Cancelled = 5
    }


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
