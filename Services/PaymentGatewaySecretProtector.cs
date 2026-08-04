using Microsoft.AspNetCore.DataProtection;

namespace Quản_lý_quán_cafe.Services;

public class PaymentGatewaySecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector =
        provider.CreateProtector("BrewPoint.PaymentGatewaySettings.v1");

    public string? Protect(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : _protector.Protect(value.Trim());
}
