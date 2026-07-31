using ChatApplicationAPI.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ChatApplicationAPI.Services.Encryption;

public class EncryptionService : IEncryptionService
{
    private readonly string _defaultKey;

    public EncryptionService(IConfiguration configuration)
    {
        _defaultKey = configuration["Jwt:Secret"] ?? "LinkUpSuperSecretEncryptionKey2026SecureJwtTokenKeyForChatApp!";
    }

    public string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            byte[] keyBytes = Derive32ByteKey(key ?? _defaultKey);
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length); // Prepend 16-byte IV

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return plainText;
        }
    }

    public string Decrypt(string cipherText, string? key = null)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            if (fullCipher.Length < 16) return cipherText;

            byte[] keyBytes = Derive32ByteKey(key ?? _defaultKey);
            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            return cipherText; // Return original if not encrypted
        }
    }

    private static byte[] Derive32ByteKey(string masterKey)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(masterKey));
    }
}
