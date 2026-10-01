using System.Globalization;
using System.Net;
using System.Text;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;
using Microsoft.Extensions.Options;

namespace CaseShop.Web.Services.Email;

public sealed class OrderConfirmationEmailService : IOrderConfirmationEmailService
{
    private readonly IOrderRepository _orders;
    private readonly IEmailSender _emailSender;
    private readonly SmtpOptions _options;
    private readonly ILogger<OrderConfirmationEmailService> _logger;

    public OrderConfirmationEmailService(
        IOrderRepository orders,
        IEmailSender emailSender,
        IOptions<SmtpOptions> options,
        ILogger<OrderConfirmationEmailService> logger)
    {
        _orders = orders;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(Guid orderId, bool allowRetry = false, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetForUpdateByIdAsync(orderId, cancellationToken);
        if (order is null || order.PaymentStatus != PaymentStatus.Paid)
        {
            return false;
        }

        if (order.ConfirmationEmailStatus == EmailDeliveryStatus.Sent ||
            (!allowRetry && order.ConfirmationEmailStatus == EmailDeliveryStatus.Sending))
        {
            return true;
        }

        order.ConfirmationEmailStatus = EmailDeliveryStatus.Sending;
        order.ConfirmationEmailAttempts++;
        order.ConfirmationEmailLastError = null;
        await _orders.UpdateAsync(order, cancellationToken);

        _logger.LogInformation(
            "Starting order confirmation email for OrderId {OrderId}, OrderCode {OrderCode}, Attempt {Attempt}",
            order.Id, order.OrderCode, order.ConfirmationEmailAttempts);

        try
        {
            await _emailSender.SendAsync(
                order.Email,
                $"CaseShop xác nhận đơn hàng {order.OrderCode}",
                BuildHtml(order),
                cancellationToken);

            order.ConfirmationEmailStatus = EmailDeliveryStatus.Sent;
            order.ConfirmationEmailSentAt = DateTime.UtcNow;
            await _orders.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order confirmation email sent for OrderId {OrderId}", order.Id);
            return true;
        }
        catch (Exception ex)
        {
            order.ConfirmationEmailStatus = EmailDeliveryStatus.Failed;
            order.ConfirmationEmailLastError = Truncate(ex.Message, 1000);

            try
            {
                await _orders.UpdateAsync(order, CancellationToken.None);
            }
            catch (Exception persistenceEx)
            {
                _logger.LogError(persistenceEx, "Could not persist email failure for OrderId {OrderId}", order.Id);
            }

            _logger.LogError(ex, "Order confirmation email failed for OrderId {OrderId}, Attempt {Attempt}",
                order.Id, order.ConfirmationEmailAttempts);
            return false;
        }
    }

    private string BuildHtml(Order order)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var rows = new StringBuilder();
        foreach (var item in order.Items)
        {
            rows.Append("<tr>")
                .Append(Cell($"{E(item.Product?.Name ?? "Sản phẩm")}{Variant(item.Variant)}"))
                .Append(Cell(item.Quantity.ToString(vi), "center"))
                .Append(Cell(Money(item.UnitPrice), "right"))
                .Append(Cell(Money(item.UnitPrice * item.Quantity), "right"))
                .Append("</tr>");
        }

        return $$"""
        <!doctype html><html lang="vi"><body style="margin:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#182230">
        <div style="max-width:680px;margin:0 auto;padding:24px">
          <div style="background:#fff;border-radius:16px;overflow:hidden;border:1px solid #e5e7eb">
            <div style="background:#166534;color:#fff;padding:24px"><h1 style="margin:0;font-size:24px">Đơn hàng đã được xác nhận</h1><p style="margin:8px 0 0">Cảm ơn bạn đã mua hàng tại CaseShop.</p></div>
            <div style="padding:24px">
              <h2 style="font-size:18px">Thông tin đơn hàng</h2>
              <p><b>Mã đơn:</b> {{E(order.OrderCode)}}<br><b>Ngày đặt:</b> {{order.CreatedAt.AddHours(7):dd/MM/yyyy HH:mm}} (GMT+7)<br><b>Trạng thái:</b> Đã thanh toán</p>
              <table style="width:100%;border-collapse:collapse" cellpadding="10"><thead><tr style="background:#f3f4f6"><th align="left">Sản phẩm</th><th>SL</th><th align="right">Đơn giá</th><th align="right">Thành tiền</th></tr></thead><tbody>{{rows}}</tbody></table>
              <div style="margin-top:20px;text-align:right"><p>Tạm tính: <b>{{Money(order.SubtotalAmount)}}</b><br>Phí giao hàng: <b>{{Money(order.ShippingFee)}}</b></p><p style="font-size:20px">Tổng thanh toán: <b>{{Money(order.TotalAmount)}}</b></p><p>Phương thức: <b>PayOS QR</b></p></div>
              <h2 style="font-size:18px">Thông tin giao hàng</h2>
              <p>{{E(order.CustomerName)}} · {{E(order.Phone)}}<br>{{E(order.Email)}}<br>{{E(order.Address)}}</p>
              <p style="margin-top:24px;padding:16px;background:#f3f4f6;border-radius:10px">Theo dõi đơn hàng bằng mã <b>{{E(order.OrderCode)}}</b> trên trang Theo dõi đơn hàng. Cần hỗ trợ, vui lòng liên hệ <a href="mailto:{{E(_options.SupportEmail)}}">{{E(_options.SupportEmail)}}</a>.</p>
            </div>
          </div>
        </div></body></html>
        """;
    }

    private static string Cell(string value, string align = "left") => $"<td align=\"{align}\" style=\"border-bottom:1px solid #e5e7eb\">{value}</td>";
    private static string Variant(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"<br><small>{E(value)}</small>";
    private static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    private static string E(string value) => WebUtility.HtmlEncode(value);
    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
