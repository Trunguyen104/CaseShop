using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services.Payments;

public interface IPaymentService
{
    Task<PaymentInfoDto> CreatePayOsPaymentAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaymentWebhookResult> ProcessPayOsWebhookAsync(PayOsWebhookDto webhook, CancellationToken cancellationToken = default);
}
