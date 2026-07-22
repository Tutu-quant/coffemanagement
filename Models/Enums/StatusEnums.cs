// Models - Enums
namespace CafeManagement.Models.Enums
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        MobileWallet,
        Bank
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }

    public enum TableStatus
    {
        Available,
        Occupied,
        Reserved,
        Maintenance
    }
}
