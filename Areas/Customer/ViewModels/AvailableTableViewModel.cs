namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class AvailableTableViewModel
{
    public int TableID { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public bool IsSelected { get; set; }
    public string CapacityDisplay => $"{Capacity} khách";
    public string FullInfo => $"Bàn {TableNumber} - {Capacity} khách{(!string.IsNullOrEmpty(Location) ? $" - {Location}" : "")}";
}
