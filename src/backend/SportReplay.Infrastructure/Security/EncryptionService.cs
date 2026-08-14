using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Options;

namespace SportReplay.Infrastructure.Security;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        var raw = options.Value.Key;
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = "sportreplay-dev-encryption-key-32";
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(aes.IV.Concat(cipher).ToArray());
    }

    public string Decrypt(string cipherText)
    {
        var payload = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = payload[..16];
        using var decryptor = aes.CreateDecryptor();
        var cipher = payload[16..];
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
