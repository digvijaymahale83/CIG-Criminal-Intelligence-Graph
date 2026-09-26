using System.Security.Cryptography;
using Application.Common.Interfaces;

namespace Infrastructure.Security;

public class Sha256HashService : IHashService
{
    public string ComputeSha256Hex(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string ComputeSha256Hex(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
