namespace PRC.Common.Validation;

public static class ImageValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly IReadOnlyList<(string MimeType, byte[] Magic)> Signatures = new[]
    {
        ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        ("image/png",  new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ("image/gif",  new byte[] { 0x47, 0x49, 0x46, 0x38 }),
        // WebP: RIFF....WEBP
        ("image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46 }),
    };

    public static (bool Valid, string? Error, string? DetectedMimeType) Validate(Stream stream, long length)
    {
        if (length == 0)
            return (false, "File is empty.", null);

        if (length > MaxSizeBytes)
            return (false, $"File exceeds the 10 MB limit (received {length / 1024 / 1024} MB).", null);

        Span<byte> header = stackalloc byte[12];
        stream.Read(header);
        stream.Seek(0, SeekOrigin.Begin);

        foreach (var (mime, magic) in Signatures)
        {
            if (header.Length < magic.Length) continue;
            if (header[..magic.Length].SequenceEqual(magic))
            {
                // WebP extra check: bytes 8-11 must be 'W','E','B','P'
                if (mime == "image/webp" && (header.Length < 12 || header[8..12].SequenceEqual("WEBP"u8) is false))
                    continue;
                return (true, null, mime);
            }
        }

        return (false, "Only JPEG, PNG, WebP and GIF images are accepted.", null);
    }
}
