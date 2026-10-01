using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace CaseShop.Web.Services.Payments;

public sealed class PayOsGateway : IPayOsGateway
{
    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsGateway> _logger;

    public PayOsGateway(IOptions<PayOsOptions> options, ILogger<PayOsGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.PayOsOrderCode is null)
        {
            throw new InvalidOperationException("Đơn hàng chưa có mã giao dịch PayOS.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var request = new CreatePaymentLinkRequest
        {
            OrderCode = order.PayOsOrderCode.Value,
            Amount = checked((long)decimal.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero)),
            Description = $"CASESHOP {order.OrderCode}"[..Math.Min(25, $"CASESHOP {order.OrderCode}".Length)],
            BuyerName = order.CustomerName,
            BuyerEmail = order.Email,
            BuyerPhone = order.Phone,
            BuyerAddress = order.Address,
            ReturnUrl = _options.ReturnUrl,
            CancelUrl = _options.CancelUrl,
            ExpiredAt = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        };

        cancellationToken.ThrowIfCancellationRequested();
        var result = await CreateClient().PaymentRequests.CreateAsync(request);
        return new PayOsPaymentLinkResult(
            result.OrderCode,
            result.PaymentLinkId,
            result.CheckoutUrl,
            result.QrCode,
            expiresAt);
    }

    public async Task<VerifiedPayOsWebhook> VerifyWebhookAsync(PayOsWebhookDto webhook, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var verified = await CreateClient().Webhooks.VerifyAsync(new Webhook
        {
            Code = webhook.Code,
            Description = webhook.Desc,
            Success = webhook.Success,
            Signature = webhook.Signature,
            Data = new WebhookData
            {
                OrderCode = webhook.Data.OrderCode,
                Amount = webhook.Data.Amount,
                Description = webhook.Data.Description,
                AccountNumber = webhook.Data.AccountNumber,
                Reference = webhook.Data.Reference,
                TransactionDateTime = webhook.Data.TransactionDateTime,
                Currency = webhook.Data.Currency,
                PaymentLinkId = webhook.Data.PaymentLinkId,
                Code = webhook.Data.Code,
                Description2 = webhook.Data.Desc,
                CounterAccountBankId = webhook.Data.CounterAccountBankId,
                CounterAccountBankName = webhook.Data.CounterAccountBankName,
                CounterAccountName = webhook.Data.CounterAccountName,
                CounterAccountNumber = webhook.Data.CounterAccountNumber,
                VirtualAccountName = webhook.Data.VirtualAccountName,
                VirtualAccountNumber = webhook.Data.VirtualAccountNumber
            }
        });

        return new VerifiedPayOsWebhook(
            webhook.Success && webhook.Code == "00" && verified.Code == "00",
            verified.OrderCode,
            verified.Amount,
            verified.PaymentLinkId,
            verified.Reference);
    }

    private PayOSClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ChecksumKey))
        {
            throw new InvalidOperationException("PayOS chưa được cấu hình trên máy chủ.");
        }

        return new PayOSClient(new PayOSOptions
        {
            ClientId = _options.ClientId,
            ApiKey = _options.ApiKey,
            ChecksumKey = _options.ChecksumKey,
            TimeoutMs = 30_000,
            MaxRetries = 2,
            Logger = _logger
        });
    }
}
