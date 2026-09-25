using System;

namespace CaseShop.Web.Entities;

public class CustomDesign
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public string PreviewImageUrl { get; set; } = null!;
    public string? OriginalImageUrl { get; set; }
    public string DesignData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
