// Only the external SDK is replaced. Tests compile the actual production storage service.
namespace CloudinaryDotNet
{
    public sealed class Account { public Account(string cloud, string key, string secret) { } }
    public sealed class ApiSettings { public bool Secure { get; set; } }
    public sealed class FileDescription(string name, Stream stream)
    {
        public string Name { get; } = name;
        public Stream Stream { get; } = stream;
    }
    public sealed class Cloudinary
    {
        public Cloudinary(Account account) { }
        public ApiSettings Api { get; } = new();
        public static int UploadCalls { get; private set; }
        public static byte[] LastBytes { get; private set; } = [];
        public static string? LastFolder { get; private set; }
        public async Task<Actions.UploadResult> UploadAsync(Actions.ImageUploadParams parameters, CancellationToken token)
        {
            UploadCalls++;
            using var copy = new MemoryStream();
            await parameters.File.Stream.CopyToAsync(copy, token);
            LastBytes = copy.ToArray();
            LastFolder = parameters.Folder;
            return new Actions.UploadResult();
        }
        public Task<Actions.UploadResult> DestroyAsync(Actions.DeletionParams parameters) =>
            Task.FromResult(new Actions.UploadResult());
    }
}
namespace CloudinaryDotNet.Actions
{
    public sealed class ImageUploadParams
    {
        public required CloudinaryDotNet.FileDescription File { get; init; }
        public string? Folder { get; init; }
        public bool Overwrite { get; init; }
    }
    public sealed class DeletionParams { public DeletionParams(string publicId) { } }
    public sealed class UploadError { public string Message { get; init; } = "Test failure"; }
    public sealed class UploadResult
    {
        public UploadError? Error { get; init; }
        public Uri SecureUrl { get; } = new("https://example.invalid/test-image.png");
        public string PublicId { get; } = "test-image";
    }
}
