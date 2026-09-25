using System;
using System.Collections.Generic;
using CaseShop.Web.Entities;

namespace CaseShop.Web.DTOs;

public class OrderTrackingDto
{
    public string OrderCode { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<OrderTrackingItemDto> Items { get; set; } = new();
}

public class OrderTrackingItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? PreviewImageUrl { get; set; }
}
