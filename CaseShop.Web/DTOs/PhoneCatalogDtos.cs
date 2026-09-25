using System.ComponentModel.DataAnnotations;

namespace CaseShop.Web.DTOs;

public class PhoneBrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PhoneBrandCreateUpdateDto
{
    [Required(ErrorMessage = "Tên thương hiệu là bắt buộc."), MaxLength(100, ErrorMessage = "Tên thương hiệu tối đa 100 ký tự.")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Slug là bắt buộc."), MaxLength(100, ErrorMessage = "Slug tối đa 100 ký tự.")] public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class PhoneModelDto
{
    public Guid Id { get; set; }
    public Guid PhoneBrandId { get; set; }
    public string PhoneBrandName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal CanvasWidth { get; set; }
    public decimal CanvasHeight { get; set; }
    public decimal CornerRadius { get; set; }
    public decimal SafeAreaX { get; set; }
    public decimal SafeAreaY { get; set; }
    public decimal SafeAreaWidth { get; set; }
    public decimal SafeAreaHeight { get; set; }
    public decimal PrintAreaX { get; set; }
    public decimal PrintAreaY { get; set; }
    public decimal PrintAreaWidth { get; set; }
    public decimal PrintAreaHeight { get; set; }
    public decimal BleedTop { get; set; }
    public decimal BleedRight { get; set; }
    public decimal BleedBottom { get; set; }
    public decimal BleedLeft { get; set; }
    public decimal CameraCutoutX { get; set; }
    public decimal CameraCutoutY { get; set; }
    public decimal CameraCutoutWidth { get; set; }
    public decimal CameraCutoutHeight { get; set; }
    public decimal CameraCutoutRadius { get; set; }
    public string? MaskImageUrl { get; set; }
    public string? OverlayImageUrl { get; set; }
}

public class PhoneModelCreateUpdateDto : IValidatableObject
{
    public Guid PhoneBrandId { get; set; }
    [Required(ErrorMessage = "Tên dòng máy là bắt buộc."), MaxLength(100, ErrorMessage = "Tên dòng máy tối đa 100 ký tự.")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Slug là bắt buộc."), MaxLength(100, ErrorMessage = "Slug tối đa 100 ký tự.")] public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal CanvasWidth { get; set; }
    public decimal CanvasHeight { get; set; }
    public decimal CornerRadius { get; set; }
    public decimal SafeAreaX { get; set; }
    public decimal SafeAreaY { get; set; }
    public decimal SafeAreaWidth { get; set; }
    public decimal SafeAreaHeight { get; set; }
    public decimal PrintAreaX { get; set; }
    public decimal PrintAreaY { get; set; }
    public decimal PrintAreaWidth { get; set; }
    public decimal PrintAreaHeight { get; set; }
    public decimal BleedTop { get; set; }
    public decimal BleedRight { get; set; }
    public decimal BleedBottom { get; set; }
    public decimal BleedLeft { get; set; }
    public decimal CameraCutoutX { get; set; }
    public decimal CameraCutoutY { get; set; }
    public decimal CameraCutoutWidth { get; set; }
    public decimal CameraCutoutHeight { get; set; }
    public decimal CameraCutoutRadius { get; set; }
    [Url, MaxLength(2048)] public string? MaskImageUrl { get; set; }
    [Url, MaxLength(2048)] public string? OverlayImageUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PhoneBrandId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn thương hiệu.", [nameof(PhoneBrandId)]);

        if (CanvasWidth <= 0) yield return Invalid("CanvasWidth phải lớn hơn 0.", nameof(CanvasWidth));
        if (CanvasHeight <= 0) yield return Invalid("CanvasHeight phải lớn hơn 0.", nameof(CanvasHeight));
        if (CornerRadius < 0) yield return Invalid("CornerRadius không được âm.", nameof(CornerRadius));
        foreach (var result in ValidateRectangle("SafeArea", SafeAreaX, SafeAreaY, SafeAreaWidth, SafeAreaHeight)) yield return result;
        foreach (var result in ValidateRectangle("PrintArea", PrintAreaX, PrintAreaY, PrintAreaWidth, PrintAreaHeight)) yield return result;
        foreach (var result in ValidateRectangle("CameraCutout", CameraCutoutX, CameraCutoutY, CameraCutoutWidth, CameraCutoutHeight)) yield return result;
        if (CameraCutoutRadius < 0) yield return Invalid("CameraCutoutRadius không được âm.", nameof(CameraCutoutRadius));
        if (BleedTop < 0 || BleedRight < 0 || BleedBottom < 0 || BleedLeft < 0)
            yield return Invalid("Các giá trị bleed không được âm.", nameof(BleedTop), nameof(BleedRight), nameof(BleedBottom), nameof(BleedLeft));
    }

    private IEnumerable<ValidationResult> ValidateRectangle(string name, decimal x, decimal y, decimal width, decimal height)
    {
        // All-zero means unconfigured — that is allowed.
        if (x == 0 && y == 0 && width == 0 && height == 0) yield break;

        if (x < 0 || y < 0 || width <= 0 || height <= 0)
            yield return Invalid($"{name} phải có tọa độ không âm và kích thước lớn hơn 0 (hoặc bỏ trống hoàn toàn).", $"{name}X", $"{name}Y", $"{name}Width", $"{name}Height");
        else if (CanvasWidth > 0 && CanvasHeight > 0 && (x + width > CanvasWidth || y + height > CanvasHeight))
            yield return Invalid($"{name} phải nằm hoàn toàn trong canvas.", $"{name}X", $"{name}Y", $"{name}Width", $"{name}Height");
    }

    private static ValidationResult Invalid(string message, params string[] members) => new(message, members);
}
