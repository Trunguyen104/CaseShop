using System;

namespace CaseShop.Web.DTOs;

public class ClientCartItem
{
    public string Key { get; set; } = Guid.NewGuid().ToString("N");
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Variant { get; set; }
    public CustomDesignInputDto? CustomDesign { get; set; }
}
