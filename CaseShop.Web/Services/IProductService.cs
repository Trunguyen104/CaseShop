using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(ProductCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, ProductCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivateProductAsync(Guid id, CancellationToken cancellationToken = default);
}
