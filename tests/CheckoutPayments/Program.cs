using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using CaseShop.Web.Services.Email;
using CaseShop.Web.Services.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Checkout payment and confirmation email tests ===");
await ValidWebhookConfirmsOrderAndTriggersEmail();
await AmountMismatchRequiresReview();
await EmailFailureDoesNotRollbackPaidOrder();
await CodOrderCanSendConfirmationEmail();
await StatusUpdateEmailIsTrackedAndDeduplicated();
await StatusEmailFailureDoesNotRollbackOrderStatus();
Console.WriteLine("PASS: all checkout payment and email assertions passed.");

static async Task ValidWebhookConfirmsOrderAndTriggersEmail()
{
    var order = NewOrder(); var email = new RecordingConfirmationEmail();
    var service = new PaymentService(new FakeOrderRepository(order), new FakePayOsGateway(150_000), email, NullLogger<PaymentService>.Instance);
    var result = await service.ProcessPayOsWebhookAsync(Webhook(order.PayOsOrderCode!.Value, 150_000));
    Assert(result.Accepted, "Valid webhook must be accepted.");
    Assert(order.PaymentStatus == PaymentStatus.Paid && order.Status == OrderStatus.Confirmed, "Valid webhook must confirm paid order.");
    Assert(email.CallCount == 1, "Confirmation email must be triggered exactly once.");
    Console.WriteLine("  ✓ valid webhook confirms order and triggers email");
}

static async Task AmountMismatchRequiresReview()
{
    var order = NewOrder(); var email = new RecordingConfirmationEmail();
    var service = new PaymentService(new FakeOrderRepository(order), new FakePayOsGateway(149_000), email, NullLogger<PaymentService>.Instance);
    var result = await service.ProcessPayOsWebhookAsync(Webhook(order.PayOsOrderCode!.Value, 149_000));
    Assert(result.Accepted, "Signed amount mismatch must be acknowledged.");
    Assert(order.PaymentStatus == PaymentStatus.RequiresReview && order.Status == OrderStatus.Pending, "Amount mismatch must not confirm order.");
    Assert(email.CallCount == 0, "Amount mismatch must not send email.");
    Console.WriteLine("  ✓ amount mismatch is isolated for review");
}

static async Task EmailFailureDoesNotRollbackPaidOrder()
{
    var order = NewOrder(); order.PaymentStatus = PaymentStatus.Paid; order.Status = OrderStatus.Confirmed;
    var service = new OrderConfirmationEmailService(
        new FakeOrderRepository(order), new ThrowingEmailSender(),
        Options.Create(new SmtpOptions { Host = "smtp.test", FromEmail = "no-reply@caseshop.test", SupportEmail = "support@caseshop.test" }),
        NullLogger<OrderConfirmationEmailService>.Instance);
    var sent = await service.SendAsync(order.Id);
    Assert(!sent, "SMTP failure must be reported.");
    Assert(order.PaymentStatus == PaymentStatus.Paid && order.Status == OrderStatus.Confirmed, "SMTP failure must not rollback payment/order.");
    Assert(order.ConfirmationEmailStatus == EmailDeliveryStatus.Failed && order.ConfirmationEmailAttempts == 1, "Failure must be persisted for retry.");
    Console.WriteLine("  ✓ email failure is recorded without rolling back payment");
}

static async Task CodOrderCanSendConfirmationEmail()
{
    var order = NewOrder();
    order.PaymentMethod = PaymentMethodType.CashOnDelivery;
    order.PaymentStatus = PaymentStatus.NotRequired;
    var sender = new RecordingEmailSender();
    var service = CreateEmailService(order, sender);

    var sent = await service.SendAsync(order.Id);

    Assert(sent, "COD confirmation email must be accepted without a paid status.");
    Assert(order.ConfirmationEmailStatus == EmailDeliveryStatus.Sent, "COD confirmation email status must be persisted as sent.");
    Assert(sender.Subjects.Single().Contains("ME-ISM", StringComparison.Ordinal), "Confirmation email must use the ME-ISM brand.");
    Assert(sender.Bodies.Single().Contains("COD", StringComparison.Ordinal), "COD payment method must be shown in the email.");
    Console.WriteLine("  ✓ COD order sends a branded confirmation email");
}

static async Task StatusUpdateEmailIsTrackedAndDeduplicated()
{
    var order = NewOrder();
    order.Status = OrderStatus.Shipping;
    order.StatusEmailStatus = EmailDeliveryStatus.Pending;
    var sender = new RecordingEmailSender();
    var service = CreateEmailService(order, sender);

    var firstSent = await service.SendStatusUpdateAsync(order.Id);
    var duplicateSent = await service.SendStatusUpdateAsync(order.Id);

    Assert(firstSent && duplicateSent, "Status email calls must complete successfully.");
    Assert(sender.Subjects.Count == 1, "The same order status email must not be sent twice.");
    Assert(order.LastNotifiedStatus == OrderStatus.Shipping, "The notified order status must be persisted.");
    Assert(order.StatusEmailStatus == EmailDeliveryStatus.Sent && order.StatusEmailAttempts == 1, "Status email delivery metadata is incorrect.");
    Console.WriteLine("  ✓ status email is tracked and deduplicated");
}

static async Task StatusEmailFailureDoesNotRollbackOrderStatus()
{
    var order = NewOrder();
    order.Status = OrderStatus.Completed;
    var service = CreateEmailService(order, new ThrowingEmailSender());

    var sent = await service.SendStatusUpdateAsync(order.Id);

    Assert(!sent, "SMTP failure must be returned for a status email.");
    Assert(order.Status == OrderStatus.Completed, "Status email failure must not rollback the order status.");
    Assert(order.StatusEmailStatus == EmailDeliveryStatus.Failed && order.StatusEmailAttempts == 1, "Status email failure must be recorded for retry.");
    Console.WriteLine("  ✓ status email failure does not rollback order status");
}

static OrderConfirmationEmailService CreateEmailService(Order order, IEmailSender sender) => new(
    new FakeOrderRepository(order), sender,
    Options.Create(new SmtpOptions { Host = "smtp.test", FromEmail = "no-reply@me-ism.test", SupportEmail = "support@me-ism.test" }),
    NullLogger<OrderConfirmationEmailService>.Instance);

static Order NewOrder() => new()
{
    Id = Guid.NewGuid(), OrderCode = "CS-261001-TEST", CustomerName = "Nguyễn Văn An", Phone = "0900000000",
    Email = "customer@example.com", Address = "1 Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh",
    AddressLine = "1 Nguyễn Huệ", ProvinceCode = 79, ProvinceName = "Thành phố Hồ Chí Minh", WardCode = 26734, WardName = "Phường Sài Gòn",
    PaymentMethod = PaymentMethodType.PayOSQr, PaymentStatus = PaymentStatus.Pending, Status = OrderStatus.Pending,
    SubtotalAmount = 150_000, TotalAmount = 150_000, PayOsOrderCode = 2610011234567,
    ConfirmationEmailStatus = EmailDeliveryStatus.Pending, CreatedAt = DateTime.UtcNow,
    StatusEmailStatus = EmailDeliveryStatus.Pending,
    Items = [new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 150_000,
        Product = new Product { Id = Guid.NewGuid(), Name = "Ốp lưng tùy chỉnh", IsActive = true } }]
};

static PayOsWebhookDto Webhook(long orderCode, long amount) => new()
{
    Code = "00", Desc = "success", Success = true, Signature = "test-signature",
    Data = new PayOsWebhookDataDto { OrderCode = orderCode, Amount = amount, Code = "00", Desc = "Thành công", PaymentLinkId = "payment-link", Reference = "bank-reference", Currency = "VND" }
};

static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed class FakePayOsGateway(long amount) : IPayOsGateway
{
    public Task<PayOsPaymentLinkResult> CreatePaymentLinkAsync(Order order, CancellationToken cancellationToken = default) => Task.FromResult(new PayOsPaymentLinkResult(order.PayOsOrderCode!.Value, "link", "https://pay.test", "qr", DateTime.UtcNow.AddMinutes(15)));
    public Task<VerifiedPayOsWebhook> VerifyWebhookAsync(PayOsWebhookDto webhook, CancellationToken cancellationToken = default) => Task.FromResult(new VerifiedPayOsWebhook(true, webhook.Data.OrderCode, amount, "payment-link", "bank-reference"));
}

sealed class RecordingConfirmationEmail : IOrderConfirmationEmailService
{
    public int CallCount { get; private set; }
    public Task<bool> SendAsync(Guid orderId, bool allowRetry = false, CancellationToken cancellationToken = default) { CallCount++; return Task.FromResult(true); }
    public Task<bool> SendStatusUpdateAsync(Guid orderId, bool allowRetry = false, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

sealed class ThrowingEmailSender : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default) => throw new InvalidOperationException("SMTP unavailable");
}

sealed class RecordingEmailSender : IEmailSender
{
    public List<string> Subjects { get; } = [];
    public List<string> Bodies { get; } = [];

    public Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        Subjects.Add(subject);
        Bodies.Add(htmlBody);
        return Task.CompletedTask;
    }
}

sealed class FakeOrderRepository(Order order) : IOrderRepository
{
    public Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Order>>([order]);
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(order.Id == id ? order : null);
    public Task<Order?> GetByOrderCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(order.OrderCode == code ? order : null);
    public Task<Order?> GetForUpdateByIdAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
    public Task<Order?> GetForUpdateByPayOsOrderCodeAsync(long code, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(order.PayOsOrderCode == code ? order : null);
    public Task<bool> ExistsByOrderCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(order.OrderCode == code);
    public Task<bool> ExistsByPayOsOrderCodeAsync(long code, CancellationToken cancellationToken = default) => Task.FromResult(order.PayOsOrderCode == code);
    public Task AddAsync(Order value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UpdateAsync(Order value, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
