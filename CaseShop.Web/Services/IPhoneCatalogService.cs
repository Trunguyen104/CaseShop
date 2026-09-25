using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services;

public interface IPhoneCatalogService
{
    Task<IReadOnlyList<PhoneBrandDto>> GetPhoneBrandsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<PhoneBrandDto?> GetPhoneBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PhoneBrandDto> CreatePhoneBrandAsync(PhoneBrandCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<PhoneBrandDto> UpdatePhoneBrandAsync(Guid id, PhoneBrandCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivatePhoneBrandAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<PhoneModelDto>> GetPhoneModelsAsync(Guid? phoneBrandId = null, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<PhoneModelDto?> GetPhoneModelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PhoneModelDto> CreatePhoneModelAsync(PhoneModelCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<PhoneModelDto> UpdatePhoneModelAsync(Guid id, PhoneModelCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivatePhoneModelAsync(Guid id, CancellationToken cancellationToken = default);
}
