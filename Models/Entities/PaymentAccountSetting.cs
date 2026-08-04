namespace Quản_lý_quán_cafe.Models.Entities;

public class PaymentAccountSetting
{
    public int PaymentAccountSettingID { get; set; }
    public string Provider { get; set; } = "Placeholder";
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
