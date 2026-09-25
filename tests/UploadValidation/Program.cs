using CaseShop.Web.Services;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Cloudinary:CloudName"] = "test-only",
    ["Cloudinary:ApiKey"] = "test-only",
    ["Cloudinary:ApiSecret"] = "test-only"
}).Build();
var storage = new CloudinaryStorageService(config, NullLogger<CloudinaryStorageService>.Instance);
var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
foreach (var (extension, mime) in new[] { ("jpg", "image/jpeg"), ("png", "image/png"), ("webp", "image/webp") })
{
    var bytes = await File.ReadAllBytesAsync(Path.Combine(fixtures, "pixel." + extension));
    var before = Cloudinary.UploadCalls;
    using var source = new NonSeekableStream(bytes);
    var result = await storage.UploadFileAsync(source, "image." + extension, mime, "caseshop/custom-stickers");
    Check(Cloudinary.UploadCalls == before + 1, "valid upload not attempted exactly once");
    Check(Cloudinary.LastBytes.SequenceEqual(bytes), "validation consumed or modified image bytes");
    Check(Cloudinary.LastFolder == "caseshop/custom-stickers", "wrong folder");
    Check(result.FileUrl.StartsWith("https://"), "not HTTPS");
}
var png = await File.ReadAllBytesAsync(Path.Combine(fixtures, "pixel.png"));
await Reject("text.jpg", "image/jpeg", "not an image"u8.ToArray());
await Reject("bytes.png", "image/png", new byte[100]);
await Reject("wrong.jpg", "image/png", png);
await Reject("wrong.jpg", "image/jpeg", png);
await Reject("empty.png", "image/png", []);
await Reject("short.png", "image/png", png[..8]);
await Reject("short.webp", "image/webp", "RIFFxxxxWEBP"u8.ToArray());
await Reject("large.png", "image/png", new byte[ImageUploadValidator.MaxBytes + 1]);
Check(!storage.IsValidImageFile("image.png", "image/jpeg", 10), "MIME mismatch accepted");
Check(!storage.IsValidImageFile("image.png", "image/png", 0), "empty metadata accepted");
Check(!storage.IsValidImageFile("image.png", "image/png", ImageUploadValidator.MaxBytes + 1), "oversize metadata accepted");
Console.WriteLine("PASS: real JPEG/PNG/WebP bytes, non-seekable streams, preserved upload bytes/folder; invalid signatures, MIME/extension mismatches, truncation, empty and oversized content rejected BEFORE SDK upload.");

async Task Reject(string name, string mime, byte[] bytes)
{
    var before = Cloudinary.UploadCalls;
    using var source = new NonSeekableStream(bytes);
    try
    {
        await storage.UploadFileAsync(source, name, mime, "caseshop/custom-stickers");
        throw new Exception("Invalid content accepted: " + name);
    }
    catch (InvalidDataException ex)
    {
        Check(ex.Message == "Invalid image. Use a JPEG, PNG or WebP file up to 5 MB.", "unsafe error");
        Check(Cloudinary.UploadCalls == before, "invalid content reached SDK upload");
    }
}
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

sealed class NonSeekableStream(byte[] bytes) : Stream
{
    private readonly MemoryStream source = new(bytes);
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => source.Read(buffer, offset, Math.Min(count, 7));
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) =>
        source.ReadAsync(buffer[..Math.Min(buffer.Length, 7)], token);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() { }
    protected override void Dispose(bool disposing) { if (disposing) source.Dispose(); base.Dispose(disposing); }
}
