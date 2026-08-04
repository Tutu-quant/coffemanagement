namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class ReservationSuccessViewModel
{
    public int ReservationID { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public int NumberOfGuests { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";

    public string TimeDisplay => ReservationDate.ToString("HH:mm");
    public string DateDisplay => ReservationDate.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("vi-VN"));
    public string GuestDisplay => $"{NumberOfGuests} khách";
    public string StatusDisplay => Status switch
    {
        "Pending" => "🟡 Chờ xác nhận",
        "Confirmed" => "🟢 Đã xác nhận",
        "Completed" => "✓ Hoàn tất",
        "Cancelled" => "✗ Đã hủy",
        _ => Status
    };
}
