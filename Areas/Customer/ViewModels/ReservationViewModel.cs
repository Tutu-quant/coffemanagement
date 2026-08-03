using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class ReservationViewModel
{
    public int ReservationID { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn bàn"), Display(Name = "Bàn")]
    public int TableID { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn thời gian"), Display(Name = "Thời gian đặt")]
    public DateTime ReservationDate { get; set; } = DateTime.Now.AddHours(1);
    [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50"), Display(Name = "Số khách")]
    public int NumberOfGuests { get; set; } = 1;
    [StringLength(500), Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
    public List<SelectListItem> Tables { get; set; } = [];
    public bool SupportsBooking => Tables.Count > 0;
}
