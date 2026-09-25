namespace CaseShop.Web.Entities;

public class PhoneModel
{
    public Guid Id { get; set; }
    public Guid PhoneBrandId { get; set; }
    public PhoneBrand PhoneBrand { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;
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
