namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class ReservationHistoryViewModel
{
    public int ReservationID { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public int NumberOfGuests { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }

    public string TimeDisplay => ReservationDate.ToString("HH:mm");
    public string DateDisplay => ReservationDate.ToString("dd/MM/yyyy");
    public string DateTimeDisplay => ReservationDate.ToString("dd/MM/yyyy HH:mm");
    public string GuestDisplay => $"{NumberOfGuests} khách";
    public string StatusBadge => Status switch
    {
        "Pending" => "badge-warning",
        "Confirmed" => "badge-success",
        "Completed" => "badge-info",
        "Cancelled" => "badge-danger",
        _ => "badge-secondary"
    };
    public string StatusText => Status switch
    {
        "Pending" => "🟡 Chờ xác nhận",
        "Confirmed" => "🟢 Đã xác nhận",
        "Completed" => "✓ Hoàn tất",
        "Cancelled" => "✗ Đã hủy",
        _ => Status
    };
    public bool CanCancel => Status == "Pending" || Status == "Confirmed";
}
