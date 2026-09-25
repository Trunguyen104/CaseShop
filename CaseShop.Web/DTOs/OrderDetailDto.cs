using System;
using System.Collections.Generic;
using CaseShop.Web.Entities;

namespace CaseShop.Web.DTOs;

public class OrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public PaymentMethodType PaymentMethod { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemDetailDto> Items { get; set; } = new();
}

public class OrderItemDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? Variant { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public CustomDesignDto? CustomDesign { get; set; }
}

public class CustomDesignDto
{
    public Guid Id { get; set; }
    public string PreviewImageUrl { get; set; } = string.Empty;
    public string? OriginalImageUrl { get; set; }
    public string DesignData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
