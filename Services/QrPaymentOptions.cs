namespace Quản_lý_quán_cafe.Services;

public sealed class QrPaymentOptions
{
    public const string SectionName = "QrPayment";

    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://payment.momo.vn/v2/gateway/api/create";
    public string RedirectUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public bool HasMerchantCredentials =>
        !string.IsNullOrWhiteSpace(PartnerCode) &&
        !string.IsNullOrWhiteSpace(AccessKey) &&
        !string.IsNullOrWhiteSpace(SecretKey);
}
