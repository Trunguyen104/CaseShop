using System;
using System.Collections.Generic;

namespace CaseShop.Web.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int ProvinceCode { get; set; }
    public string ProvinceName { get; set; } = null!;
    public int WardCode { get; set; }
    public string WardName { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public PaymentMethodType PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public long? PayOsOrderCode { get; set; }
    public string? PayOsPaymentLinkId { get; set; }
    public string? PayOsCheckoutUrl { get; set; }
    public string? PayOsQrCode { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaymentExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public EmailDeliveryStatus ConfirmationEmailStatus { get; set; }
    public int ConfirmationEmailAttempts { get; set; }
    public DateTime? ConfirmationEmailSentAt { get; set; }
    public string? ConfirmationEmailLastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
