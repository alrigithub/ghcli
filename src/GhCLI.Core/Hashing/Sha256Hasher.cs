using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GhCLI.Protocol;

namespace GhCLI.Core.Hashing;

public static class Sha256Hasher
{
    public static string HashText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ShortHash(string text, int length = 16)
    {
        var full = HashText(text);
        return full[..Math.Min(length, full.Length)];
    }

    public static string HashObject(object? value)
    {
        var json = JsonSerializer.Serialize(value, ProtocolJson.Options);
        return HashText(json);
    }
}
