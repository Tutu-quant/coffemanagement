namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class ReservationSummaryViewModel
{
    public int? SelectedTableId { get; set; }
    public string? SelectedTableNumber { get; set; }
    public DateTime ReservationDate { get; set; }
    public int NumberOfGuests { get; set; }
    public int TableCapacity { get; set; }
    public string Status { get; set; } = "Chờ xác nhận";
    public string? Notes { get; set; }
    public bool IsComplete { get; set; }

    public string TimeDisplay => ReservationDate.ToString("HH:mm");
    public string DateDisplay => ReservationDate.ToString("dd MMMM yyyy");
    public string GuestDisplay => $"{NumberOfGuests} người";
    public string CapacityStatus => NumberOfGuests <= TableCapacity ? "✓ Đủ chỗ" : "✗ Vượt quá";
}
