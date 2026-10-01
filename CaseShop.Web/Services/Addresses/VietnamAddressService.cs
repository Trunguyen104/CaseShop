using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CaseShop.Web.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CaseShop.Web.Services.Addresses;

public sealed class VietnamAddressService : IVietnamAddressService
{
    private const string ProvincesCacheKey = "vietnam-addresses:provinces:v2";
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VietnamAddressService> _logger;

    public VietnamAddressService(HttpClient httpClient, IMemoryCache cache, ILogger<VietnamAddressService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VietnamProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ProvincesCacheKey, out IReadOnlyList<VietnamProvinceDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var data = await _httpClient.GetFromJsonAsync<List<ProvinceResponse>>("p/", cancellationToken) ?? [];
            var result = data.Select(x => new VietnamProvinceDto(x.Code, x.Name, x.DivisionType)).ToList();
            _cache.Set(ProvincesCacheKey, result, TimeSpan.FromHours(12));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load current Vietnam province catalog");
            throw new InvalidOperationException("Không thể tải danh mục Tỉnh/Thành phố lúc này.", ex);
        }
    }

    public async Task<IReadOnlyList<VietnamWardDto>> GetWardsAsync(int provinceCode, CancellationToken cancellationToken = default)
    {
        if (provinceCode <= 0)
        {
            return [];
        }

        var cacheKey = $"vietnam-addresses:wards:v2:{provinceCode}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<VietnamWardDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var data = await _httpClient.GetFromJsonAsync<List<WardResponse>>($"w/?province={provinceCode}", cancellationToken) ?? [];
            var result = data.Select(x => new VietnamWardDto(x.Code, x.Name, x.DivisionType, x.ProvinceCode)).ToList();
            _cache.Set(cacheKey, result, TimeSpan.FromHours(12));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load Vietnam ward catalog for ProvinceCode {ProvinceCode}", provinceCode);
            throw new InvalidOperationException("Không thể tải danh mục Phường/Xã lúc này.", ex);
        }
    }

    public async Task<bool> IsValidAsync(int provinceCode, int wardCode, CancellationToken cancellationToken = default)
    {
        var wards = await GetWardsAsync(provinceCode, cancellationToken);
        return wards.Any(x => x.Code == wardCode && x.ProvinceCode == provinceCode);
    }

    private sealed class ProvinceResponse
    {
        public int Code { get; init; }
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("division_type")] public string DivisionType { get; init; } = string.Empty;
    }

    private sealed class WardResponse
    {
        public int Code { get; init; }
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("division_type")] public string DivisionType { get; init; } = string.Empty;
        [JsonPropertyName("province_code")] public int ProvinceCode { get; init; }
    }
}
