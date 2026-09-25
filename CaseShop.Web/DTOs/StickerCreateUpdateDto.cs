using System.ComponentModel.DataAnnotations;

namespace CaseShop.Web.DTOs;

public class StickerCreateUpdateDto
{
    [Required(ErrorMessage = "Tên sticker là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên sticker tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }

    public bool IsActive { get; set; } = true;
}
