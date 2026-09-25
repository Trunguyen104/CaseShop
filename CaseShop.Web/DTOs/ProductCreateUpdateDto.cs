using System.ComponentModel.DataAnnotations;

namespace CaseShop.Web.DTOs;

public class ProductCreateUpdateDto
{
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
    public string? Description { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }

    public bool IsActive { get; set; } = true;
}
