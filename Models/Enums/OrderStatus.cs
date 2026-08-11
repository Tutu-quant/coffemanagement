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

        /// <summary>
        /// Statuses where items can be added to order
        /// </summary>
        public static readonly HashSet<string> AddableStatuses = new() { Pending, Preparing };

        /// <summary>
        /// Status where items can be fully edited (add/update/remove)
        /// </summary>
        public static readonly HashSet<string> FullyEditableStatuses = new() { Pending };

        /// <summary>
        /// Statuses that can be paid for
        /// </summary>
        public static readonly HashSet<string> PayableStatuses = new() { Ready, WaitingPayment };

        /// <summary>
        /// Statuses that are final/locked
        /// </summary>
        public static readonly HashSet<string> ClosedStatuses = new() { Completed, Cancelled };

        /// <summary>
        /// Check if order is open for adding items
        /// </summary>
        public static bool IsAddable(string? status) => !string.IsNullOrEmpty(status) && AddableStatuses.Contains(status);

        /// <summary>
        /// Check if order is fully editable (Pending only)
        /// </summary>
        public static bool IsFullyEditable(string? status) => !string.IsNullOrEmpty(status) && FullyEditableStatuses.Contains(status);

        /// <summary>
        /// Check if order can receive new items (Pending/Preparing, but for Preparing only Add, not Update/Remove)
        /// </summary>
        public static bool CanAddItems(string? status) => !string.IsNullOrEmpty(status) && AddableStatuses.Contains(status);

        /// <summary>
        /// Check if order is closed
        /// </summary>
        public static bool IsClosed(string? status) => !string.IsNullOrEmpty(status) && ClosedStatuses.Contains(status);

        /// <summary>
        /// Check if order is payable
        /// </summary>
        public static bool IsPayable(string? status) => !string.IsNullOrEmpty(status) && PayableStatuses.Contains(status);
    }
}
