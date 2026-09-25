using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CaseShop.Web.Services;

public class CloudinaryStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(IConfiguration configuration, ILogger<CloudinaryStorageService> logger)
    {
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var apiKey = configuration["Cloudinary:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var apiSecret = configuration["Cloudinary:ApiSecret"]
            ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogWarning("Cloudinary credentials are not fully configured.");
            // We initialize it with empty string to avoid immediate crash, but uploads will fail if not configured.
            var account = new Account(cloudName ?? "", apiKey ?? "", apiSecret ?? "");
            _cloudinary = new Cloudinary(account);
        }
        else
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
    }

    public async Task<(string FileUrl, string PublicId)> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "caseshop/products", CancellationToken cancellationToken = default)
    {
        await using var validatedStream = await ImageUploadValidator.ReadValidatedAsync(
            fileStream, fileName, contentType, cancellationToken);
        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant(), validatedStream),
            Folder = folder,
            Overwrite = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            throw new Exception($"Lỗi khi tải ảnh lên: {uploadResult.Error.Message}");
        }

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    public async Task DeleteFileAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return; // Nothing to delete
        }

        var deletionParams = new DeletionParams(publicId);
        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

        if (deletionResult.Error != null)
        {
            _logger.LogError("Cloudinary deletion failed for {PublicId}: {Error}", publicId, deletionResult.Error.Message);
            // We usually do not throw here to prevent blocking an update if delete fails
        }
    }

    public bool IsValidImageFile(string fileName, string contentType, long fileSizeBytes)
    {
        return ImageUploadValidator.IsValidMetadata(fileName, contentType, fileSizeBytes);
    }
}
