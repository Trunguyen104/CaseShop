using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services.Addresses;

public interface IVietnamAddressService
{
    Task<IReadOnlyList<VietnamProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VietnamWardDto>> GetWardsAsync(int provinceCode, CancellationToken cancellationToken = default);
    Task<bool> IsValidAsync(int provinceCode, int wardCode, CancellationToken cancellationToken = default);
}
