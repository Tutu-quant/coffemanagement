namespace Quản_lý_quán_cafe.Models.Entities;

public class PaymentGatewaySetting
{
    public int PaymentGatewaySettingID { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string? ApiKeyProtected { get; set; }
    public string? SecretKeyProtected { get; set; }
    public string? Endpoint { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
