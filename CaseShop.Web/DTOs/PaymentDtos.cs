using System.ComponentModel.DataAnnotations;
using CaseShop.Web.Entities;

namespace CaseShop.Web.DTOs;

public sealed class PaymentInfoDto
{
    public PaymentStatus Status { get; init; }
    public long? ProviderOrderCode { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? QrCode { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed class PayOsWebhookDto
{
    [Required, MaxLength(10)]
    public string Code { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Desc { get; init; } = string.Empty;

    public bool Success { get; init; }

    [Required]
    public PayOsWebhookDataDto Data { get; init; } = new();

    [Required, MaxLength(256)]
    public string Signature { get; init; } = string.Empty;
}

public sealed class PayOsWebhookDataDto
{
    public long OrderCode { get; init; }
    [Range(1, long.MaxValue)] public long Amount { get; init; }
    [MaxLength(50)] public string Description { get; init; } = string.Empty;
    [MaxLength(50)] public string AccountNumber { get; init; } = string.Empty;
    [MaxLength(100)] public string Reference { get; init; } = string.Empty;
    [MaxLength(40)] public string TransactionDateTime { get; init; } = string.Empty;
    [MaxLength(10)] public string Currency { get; init; } = string.Empty;
    [MaxLength(100)] public string PaymentLinkId { get; init; } = string.Empty;
    [MaxLength(10)] public string Code { get; init; } = string.Empty;
    [MaxLength(200)] public string Desc { get; init; } = string.Empty;
    [MaxLength(50)] public string CounterAccountBankId { get; init; } = string.Empty;
    [MaxLength(200)] public string CounterAccountBankName { get; init; } = string.Empty;
    [MaxLength(200)] public string CounterAccountName { get; init; } = string.Empty;
    [MaxLength(50)] public string CounterAccountNumber { get; init; } = string.Empty;
    [MaxLength(200)] public string VirtualAccountName { get; init; } = string.Empty;
    [MaxLength(50)] public string VirtualAccountNumber { get; init; } = string.Empty;
}

public sealed record PaymentWebhookResult(bool Accepted, string Message);
