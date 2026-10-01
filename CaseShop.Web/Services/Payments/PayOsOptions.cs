using System.ComponentModel.DataAnnotations;

namespace CaseShop.Web.Services.Payments;

public sealed class PayOsOptions
{
    public const string SectionName = "PayOS";

    [Required] public string ClientId { get; set; } = string.Empty;
    [Required] public string ApiKey { get; set; } = string.Empty;
    [Required] public string ChecksumKey { get; set; } = string.Empty;
    [Required, Url] public string ReturnUrl { get; set; } = string.Empty;
    [Required, Url] public string CancelUrl { get; set; } = string.Empty;
    [Range(5, 60)] public int ExpirationMinutes { get; set; } = 15;
}
