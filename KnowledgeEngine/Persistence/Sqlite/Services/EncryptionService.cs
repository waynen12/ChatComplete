using System.Security.Cryptography;
using System.Text;

namespace KnowledgeEngine.Persistence.Sqlite.Services;

/// <summary>
/// AES-256 encryption service for protecting sensitive configuration data
/// Uses application-specific key for encryption/decryption
/// </summary>
public class EncryptionService
{
    private static readonly byte[] _key = DeriveKeyFromString("AI-Knowledge-Manager-2025");
    private static readonly byte[] _iv = new byte[16]; // Zero IV for simplicity - consider random IV for production

    /// <summary>
    /// Encrypts sensitive text data using AES-256
    /// </summary>
    public static byte[] Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return Array.Empty<byte>();

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cs);

        writer.Write(plaintext);
        writer.Close();

        return ms.ToArray();
    }

    /// <summary>
    /// Decrypts sensitive data back to plaintext
    /// </summary>
    public static string Decrypt(byte[] ciphertext)
    {
        if (ciphertext == null || ciphertext.Length == 0)
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Derives a 256-bit key from a string using PBKDF2
    /// </summary>
    private static byte[] DeriveKeyFromString(string keyString)
    {
        var salt = Encoding.UTF8.GetBytes("SQLite-Knowledge-Salt-2025");
        using var pbkdf2 = new Rfc2898DeriveBytes(keyString, salt, 100000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256-bit key
    }
}