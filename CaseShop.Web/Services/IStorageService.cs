using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CaseShop.Web.Services;

public interface IStorageService
{
    Task<(string FileUrl, string PublicId)> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "caseshop/products", CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string publicId, CancellationToken cancellationToken = default);
    bool IsValidImageFile(string fileName, string contentType, long fileSizeBytes);
}
