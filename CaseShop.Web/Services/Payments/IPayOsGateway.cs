using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;

namespace CaseShop.Web.Services.Payments;

public interface IPayOsGateway
{
    Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(Order order, CancellationToken cancellationToken = default);
    Task<VerifiedPayOsWebhook> VerifyWebhookAsync(PayOsWebhookDto webhook, CancellationToken cancellationToken = default);
}

public sealed record PayOsPaymentLinkResult(
    long OrderCode,
    string PaymentLinkId,
    string CheckoutUrl,
    string QrCode,
    DateTime ExpiresAt);

public sealed record VerifiedPayOsWebhook(
    bool Success,
    long OrderCode,
    long Amount,
    string PaymentLinkId,
    string Reference);
