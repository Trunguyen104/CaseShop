using System.Buffers.Binary;

namespace CaseShop.Web.Services;

/// <summary>Bounded metadata and signature checks; this is not a full image decoder.</summary>
public static class ImageUploadValidator
{
    public const int MaxBytes = 5 * 1024 * 1024;
    private const string InvalidMessage = "Invalid image. Use a JPEG, PNG or WebP file up to 5 MB.";

    public static bool IsValidMetadata(string fileName, string contentType, long size)
    {
        if (size <= 0 || size > MaxBytes) return false;
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase),
            ".png" => string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase),
            ".webp" => string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public static async Task<MemoryStream> ReadValidatedAsync(Stream source, string fileName,
        string contentType, CancellationToken cancellationToken = default)
    {
        // Do not trust Length/seek support or the size declared by the browser.
        if (!IsValidMetadata(fileName, contentType, 1)) throw new InvalidDataException(InvalidMessage);
        var buffer = new MemoryStream();
        try
        {
            var chunk = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(chunk.AsMemory(0,
                (int)Math.Min(chunk.Length, MaxBytes + 1L - buffer.Length)), cancellationToken)) > 0)
            {
                if (buffer.Length + count > MaxBytes) throw new InvalidDataException(InvalidMessage);
                await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
            }
            if (!HasMatchingSignature(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), contentType))
                throw new InvalidDataException(InvalidMessage);
            buffer.Position = 0;
            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    private static bool HasMatchingSignature(ReadOnlySpan<byte> bytes, string contentType)
    {
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff
                && bytes[3] != 0 && bytes[3] != 0xff;
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 33 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                && BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) == 13
                && bytes.Slice(12, 4).SequenceEqual("IHDR"u8)
                && BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4)) > 0
                && BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4)) > 0;
        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 20 && bytes[..4].SequenceEqual("RIFF"u8)
                && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
                && (long)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4)) + 8 == bytes.Length
                && (bytes.Slice(12, 4).SequenceEqual("VP8 "u8)
                    || bytes.Slice(12, 4).SequenceEqual("VP8L"u8)
                    || bytes.Slice(12, 4).SequenceEqual("VP8X"u8))
                && BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(16, 4)) > 0
                && BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(16, 4)) <= bytes.Length - 20;
        return false;
    }
}
