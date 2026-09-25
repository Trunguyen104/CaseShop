using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;

namespace CaseShop.Web.Services;

public interface IStickerService
{
    Task<IReadOnlyList<StickerDto>> GetActiveStickersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StickerDto>> GetAllStickersAsync(CancellationToken cancellationToken = default);
    Task<StickerDto?> GetStickerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StickerDto> CreateStickerAsync(StickerCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<StickerDto> UpdateStickerAsync(Guid id, StickerCreateUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeactivateStickerAsync(Guid id, CancellationToken cancellationToken = default);
}
