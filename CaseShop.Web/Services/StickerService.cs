using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaseShop.Web.DTOs;
using CaseShop.Web.Entities;
using CaseShop.Web.Repositories;

namespace CaseShop.Web.Services;

public class StickerService : IStickerService
{
    private readonly IStickerRepository _stickerRepository;

    public StickerService(IStickerRepository stickerRepository)
    {
        _stickerRepository = stickerRepository;
    }

    public async Task<IReadOnlyList<StickerDto>> GetActiveStickersAsync(CancellationToken cancellationToken = default)
    {
        var stickers = await _stickerRepository.GetAllAsync(activeOnly: true, cancellationToken);
        return stickers.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<StickerDto>> GetAllStickersAsync(CancellationToken cancellationToken = default)
    {
        var stickers = await _stickerRepository.GetAllAsync(activeOnly: false, cancellationToken);
        return stickers.Select(MapToDto).ToList();
    }

    public async Task<StickerDto?> GetStickerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sticker = await _stickerRepository.GetByIdAsync(id, cancellationToken);
        return sticker == null ? null : MapToDto(sticker);
    }

    public async Task<StickerDto> CreateStickerAsync(StickerCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Tên sticker không được để trống.", nameof(dto.Name));
        }



        var sticker = new Sticker
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            ImagePublicId = dto.ImagePublicId?.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _stickerRepository.AddAsync(sticker, cancellationToken);
        return MapToDto(sticker);
    }

    public async Task<StickerDto> UpdateStickerAsync(Guid id, StickerCreateUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var sticker = await _stickerRepository.GetByIdAsync(id, cancellationToken);
        if (sticker == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sticker với mã: {id}");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Tên sticker không được để trống.", nameof(dto.Name));
        }



        sticker.Name = dto.Name.Trim();
        sticker.ImageUrl = dto.ImageUrl?.Trim();
        sticker.ImagePublicId = dto.ImagePublicId?.Trim();
        sticker.IsActive = dto.IsActive;
        sticker.UpdatedAt = DateTime.UtcNow;

        await _stickerRepository.UpdateAsync(sticker, cancellationToken);
        return MapToDto(sticker);
    }

    public async Task<bool> DeactivateStickerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sticker = await _stickerRepository.GetByIdAsync(id, cancellationToken);
        if (sticker == null || !sticker.IsActive)
        {
            return false;
        }

        sticker.IsActive = false;
        sticker.UpdatedAt = DateTime.UtcNow;

        await _stickerRepository.UpdateAsync(sticker, cancellationToken);
        return true;
    }

    private static StickerDto MapToDto(Sticker sticker)
    {
        return new StickerDto
        {
            Id = sticker.Id,
            Name = sticker.Name,
            ImageUrl = sticker.ImageUrl,
            ImagePublicId = sticker.ImagePublicId,
            IsActive = sticker.IsActive,
            CreatedAt = sticker.CreatedAt,
            UpdatedAt = sticker.UpdatedAt
        };
    }
}
