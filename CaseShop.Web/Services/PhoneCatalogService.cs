using System.ComponentModel.DataAnnotations;
using CaseShop.Web.Data;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseShop.Web.Services;

public class PhoneCatalogService : IPhoneCatalogService
{
    private readonly AppDbContext _context;
    
    public PhoneCatalogService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<PhoneBrandDto>> GetPhoneBrandsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.PhoneBrands.AsNoTracking();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).Select(x => Map(x)).ToListAsync(cancellationToken);
    }
    
    public async Task<PhoneBrandDto?> GetPhoneBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PhoneBrands.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : Map(entity);
    }
    
    public async Task<PhoneBrandDto> CreatePhoneBrandAsync(PhoneBrandCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var slug = NormalizeSlug(dto.Slug);
        if (await _context.PhoneBrands.AnyAsync(x => x.Slug == slug, cancellationToken)) 
            throw Duplicate("Slug thương hiệu đã tồn tại.");
            
        var entity = new PhoneBrand { Id = Guid.NewGuid(), Name = dto.Name.Trim(), Slug = slug, IsActive = dto.IsActive };
        _context.PhoneBrands.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }
    
    public async Task<PhoneBrandDto> UpdatePhoneBrandAsync(Guid id, PhoneBrandCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        var entity = await _context.PhoneBrands.FindAsync([id], cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy thương hiệu.");
        var slug = NormalizeSlug(dto.Slug);
        
        if (await _context.PhoneBrands.AnyAsync(x => x.Slug == slug && x.Id != id, cancellationToken)) 
            throw Duplicate("Slug thương hiệu đã tồn tại.");
            
        entity.Name = dto.Name.Trim();
        entity.Slug = slug;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }
    
    public async Task<bool> DeactivatePhoneBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PhoneBrands.FindAsync([id], cancellationToken);
        if (entity == null) return false;
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PhoneModelDto>> GetPhoneModelsAsync(Guid? phoneBrandId = null, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.PhoneModels.AsNoTracking().Include(x => x.PhoneBrand).AsQueryable();
        if (phoneBrandId.HasValue) query = query.Where(x => x.PhoneBrandId == phoneBrandId);
        if (activeOnly) query = query.Where(x => x.IsActive && x.PhoneBrand.IsActive);
        
        var list = await query.OrderBy(x => x.PhoneBrand.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }
    
    public async Task<PhoneModelDto?> GetPhoneModelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PhoneModels.AsNoTracking().Include(x => x.PhoneBrand).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : Map(entity);
    }
    
    public async Task<PhoneModelDto> CreatePhoneModelAsync(PhoneModelCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        
        var brand = await _context.PhoneBrands.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PhoneBrandId, cancellationToken) 
            ?? throw new KeyNotFoundException("Không tìm thấy thương hiệu.");
        if (!brand.IsActive) throw new InvalidOperationException("Thương hiệu đã ngừng hoạt động.");
            
        var slug = NormalizeSlug(dto.Slug);
        if (await _context.PhoneModels.AnyAsync(x => x.PhoneBrandId == dto.PhoneBrandId && x.Slug == slug, cancellationToken)) 
            throw Duplicate("Slug dòng máy đã tồn tại trong thương hiệu.");
            
        var entity = new PhoneModel { Id = Guid.NewGuid() };
        Apply(entity, dto, slug);
        _context.PhoneModels.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return (await GetPhoneModelAsync(entity.Id, cancellationToken))!;
    }
    
    public async Task<PhoneModelDto> UpdatePhoneModelAsync(Guid id, PhoneModelCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        
        var entity = await _context.PhoneModels.Include(x => x.PhoneBrand).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) 
            ?? throw new KeyNotFoundException("Không tìm thấy dòng máy.");
            
        if (entity.PhoneBrandId != dto.PhoneBrandId)
        {
            var brand = await _context.PhoneBrands.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PhoneBrandId, cancellationToken) 
                ?? throw new KeyNotFoundException("Không tìm thấy thương hiệu.");
            if (!brand.IsActive) throw new InvalidOperationException("Thương hiệu đã ngừng hoạt động.");
        }
        
        var slug = NormalizeSlug(dto.Slug);
        if (await _context.PhoneModels.AnyAsync(x => x.PhoneBrandId == dto.PhoneBrandId && x.Slug == slug && x.Id != id, cancellationToken)) 
            throw Duplicate("Slug dòng máy đã tồn tại trong thương hiệu.");
            
        Apply(entity, dto, slug);
        await _context.SaveChangesAsync(cancellationToken);
        
        return (await GetPhoneModelAsync(entity.Id, cancellationToken))!;
    }
    
    public async Task<bool> DeactivatePhoneModelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PhoneModels.FindAsync([id], cancellationToken);
        if (entity == null) return false;
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Validate(object dto) => Validator.ValidateObject(dto, new ValidationContext(dto), true);
    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
    private static InvalidOperationException Duplicate(string message) => new(message);

    private static void Apply(PhoneModel x, PhoneModelCreateUpdateDto d, string slug)
    {
        x.PhoneBrandId = d.PhoneBrandId;
        x.Name = d.Name.Trim();
        x.Slug = slug;
        x.IsActive = d.IsActive;
        
        x.CanvasWidth = d.CanvasWidth; x.CanvasHeight = d.CanvasHeight; x.CornerRadius = d.CornerRadius;
        x.SafeAreaX = d.SafeAreaX; x.SafeAreaY = d.SafeAreaY; x.SafeAreaWidth = d.SafeAreaWidth; x.SafeAreaHeight = d.SafeAreaHeight;
        x.PrintAreaX = d.PrintAreaX; x.PrintAreaY = d.PrintAreaY; x.PrintAreaWidth = d.PrintAreaWidth; x.PrintAreaHeight = d.PrintAreaHeight;
        x.BleedTop = d.BleedTop; x.BleedRight = d.BleedRight; x.BleedBottom = d.BleedBottom; x.BleedLeft = d.BleedLeft;
        x.CameraCutoutX = d.CameraCutoutX; x.CameraCutoutY = d.CameraCutoutY; x.CameraCutoutWidth = d.CameraCutoutWidth; x.CameraCutoutHeight = d.CameraCutoutHeight; x.CameraCutoutRadius = d.CameraCutoutRadius;
        
        x.MaskImageUrl = string.IsNullOrWhiteSpace(d.MaskImageUrl) ? null : d.MaskImageUrl.Trim(); 
        x.OverlayImageUrl = string.IsNullOrWhiteSpace(d.OverlayImageUrl) ? null : d.OverlayImageUrl.Trim();
    }
    
    private static PhoneBrandDto Map(PhoneBrand x) => new() { Id = x.Id, Name = x.Name, Slug = x.Slug, IsActive = x.IsActive, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt };
    private static PhoneModelDto Map(PhoneModel x) => new() 
    { 
        Id = x.Id, 
        PhoneBrandId = x.PhoneBrandId, 
        PhoneBrandName = x.PhoneBrand?.Name ?? "", 
        Name = x.Name, 
        Slug = x.Slug, 
        IsActive = x.IsActive, 
        CreatedAt = x.CreatedAt, 
        UpdatedAt = x.UpdatedAt,
        CanvasWidth = x.CanvasWidth, 
        CanvasHeight = x.CanvasHeight, 
        CornerRadius = x.CornerRadius, 
        SafeAreaX = x.SafeAreaX, 
        SafeAreaY = x.SafeAreaY, 
        SafeAreaWidth = x.SafeAreaWidth, 
        SafeAreaHeight = x.SafeAreaHeight, 
        PrintAreaX = x.PrintAreaX, 
        PrintAreaY = x.PrintAreaY, 
        PrintAreaWidth = x.PrintAreaWidth, 
        PrintAreaHeight = x.PrintAreaHeight, 
        BleedTop = x.BleedTop, 
        BleedRight = x.BleedRight, 
        BleedBottom = x.BleedBottom, 
        BleedLeft = x.BleedLeft, 
        CameraCutoutX = x.CameraCutoutX,
        CameraCutoutY = x.CameraCutoutY,
        CameraCutoutWidth = x.CameraCutoutWidth,
        CameraCutoutHeight = x.CameraCutoutHeight,
        CameraCutoutRadius = x.CameraCutoutRadius,
        MaskImageUrl = x.MaskImageUrl, 
        OverlayImageUrl = x.OverlayImageUrl
    };
}
