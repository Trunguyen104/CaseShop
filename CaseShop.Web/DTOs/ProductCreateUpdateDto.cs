using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CaseShop.Web.DTOs;

public class ProductCreateUpdateDto
{
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "1", "1000000000", ErrorMessage = "Giá sản phẩm phải từ 1₫ đến 1.000.000.000₫.")]
    public decimal Price { get; set; }

    [JsonIgnore]
    [Range(1, 1000000, ErrorMessage = "Giá sản phẩm phải từ 1 đến 1.000.000 nghìn đồng.")]
    public int PriceInThousands
    {
        get => decimal.ToInt32(decimal.Truncate(Price / 1000m));
        set => Price = value * 1000m;
    }

    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }

    public bool IsActive { get; set; } = true;
}
