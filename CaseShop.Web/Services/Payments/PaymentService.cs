using System.Security.Cryptography;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services.Email;

namespace CaseShop.Web.Services.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orders;
    private readonly IPayOsGateway _gateway;
    private readonly IOrderConfirmationEmailService _confirmationEmail;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orders,
        IPayOsGateway gateway,
        IOrderConfirmationEmailService confirmationEmail,
        ILogger<PaymentService> logger)
    {
        _orders = orders;
        _gateway = gateway;
        _confirmationEmail = confirmationEmail;
        _logger = logger;
    }

    public async Task<PaymentInfoDto> CreatePayOsPaymentAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting PayOS payment creation for OrderId {OrderId}", orderId);
        var order = await _orders.GetForUpdateByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy đơn hàng cần thanh toán.");

        if (order.PaymentMethod != PaymentMethodType.PayOSQr)
        {
            throw new InvalidOperationException("Đơn hàng không sử dụng phương thức PayOS QR.");
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return MapPayment(order);
        }

        if (order.PaymentStatus == PaymentStatus.Pending &&
            order.PaymentExpiresAt > DateTime.UtcNow &&
            !string.IsNullOrWhiteSpace(order.PayOsCheckoutUrl))
        {
            return MapPayment(order);
        }

        order.PayOsOrderCode = await GeneratePayOsOrderCodeAsync(cancellationToken);
        order.PayOsPaymentLinkId = null;
        order.PayOsCheckoutUrl = null;
        order.PayOsQrCode = null;
        order.PaymentExpiresAt = null;
        order.PaymentStatus = PaymentStatus.Pending;
        await _orders.UpdateAsync(order, cancellationToken);

        try
        {
            var link = await _gateway.CreatePaymentLinkAsync(order, cancellationToken);
            order.PayOsPaymentLinkId = link.PaymentLinkId;
            order.PayOsCheckoutUrl = link.CheckoutUrl;
            order.PayOsQrCode = link.QrCode;
            order.PaymentExpiresAt = link.ExpiresAt;
            await _orders.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("PayOS payment link created for OrderId {OrderId}, ProviderOrderCode {ProviderOrderCode}", order.Id, link.OrderCode);
            return MapPayment(order);
        }
        catch (Exception ex)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            await _orders.UpdateAsync(order, CancellationToken.None);
            _logger.LogError(ex, "PayOS payment creation failed for OrderId {OrderId}", order.Id);
            throw new InvalidOperationException("Không thể tạo mã QR thanh toán lúc này. Vui lòng thử lại.", ex);
        }
    }

    public async Task<PaymentWebhookResult> ProcessPayOsWebhookAsync(PayOsWebhookDto webhook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webhook);
        _logger.LogInformation("Starting PayOS webhook processing for ProviderOrderCode {ProviderOrderCode}", webhook.Data.OrderCode);

        VerifiedPayOsWebhook verified;
        try
        {
            verified = await _gateway.VerifyWebhookAsync(webhook, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rejected invalid PayOS webhook for ProviderOrderCode {ProviderOrderCode}", webhook.Data.OrderCode);
            return new PaymentWebhookResult(false, "Webhook PayOS không hợp lệ.");
        }

        if (!verified.Success)
        {
            return new PaymentWebhookResult(true, "Đã ghi nhận thông báo chưa thành công.");
        }

        var order = await _orders.GetForUpdateByPayOsOrderCodeAsync(verified.OrderCode, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("PayOS webhook references unknown ProviderOrderCode {ProviderOrderCode}", verified.OrderCode);
            return new PaymentWebhookResult(false, "Không tìm thấy đơn hàng tương ứng.");
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            if (order.ConfirmationEmailStatus == EmailDeliveryStatus.Failed)
            {
                await _confirmationEmail.SendAsync(order.Id, allowRetry: true, cancellationToken);
            }
            return new PaymentWebhookResult(true, "Giao dịch đã được xử lý trước đó.");
        }

        var expectedAmount = decimal.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero);
        if (verified.Amount != expectedAmount)
        {
            order.PaymentStatus = PaymentStatus.RequiresReview;
            order.PaymentReference = verified.Reference;
            await _orders.UpdateAsync(order, cancellationToken);
            _logger.LogWarning("PayOS amount mismatch for OrderId {OrderId}: expected {ExpectedAmount}, received {ReceivedAmount}",
                order.Id, expectedAmount, verified.Amount);
            return new PaymentWebhookResult(true, "Giao dịch cần được đối soát do sai số tiền.");
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status = OrderStatus.Confirmed;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentReference = verified.Reference;
        order.PayOsPaymentLinkId = verified.PaymentLinkId;
        order.ConfirmationEmailStatus = EmailDeliveryStatus.Pending;
        await _orders.UpdateAsync(order, cancellationToken);

        await _confirmationEmail.SendAsync(order.Id, cancellationToken: cancellationToken);
        _logger.LogInformation("PayOS webhook completed for OrderId {OrderId}", order.Id);
        return new PaymentWebhookResult(true, "Đã xác nhận thanh toán.");
    }

    private async Task<long> GeneratePayOsOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 10_000_000_000;
            var candidate = timestamp * 1000 + RandomNumberGenerator.GetInt32(100, 1000);
            if (!await _orders.ExistsByPayOsOrderCodeAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Không thể tạo mã giao dịch thanh toán duy nhất.");
    }

    private static PaymentInfoDto MapPayment(Order order) => new()
    {
        Status = order.PaymentStatus,
        ProviderOrderCode = order.PayOsOrderCode,
        CheckoutUrl = order.PayOsCheckoutUrl,
        QrCode = order.PayOsQrCode,
        ExpiresAt = order.PaymentExpiresAt,
        PaidAt = order.PaidAt
    };
}
