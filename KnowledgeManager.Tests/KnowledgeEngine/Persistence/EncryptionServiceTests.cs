using KnowledgeEngine.Persistence.Sqlite.Services;

namespace KnowledgeManager.Tests.KnowledgeEngine.Persistence;

/// <summary>
/// Tests for AES-256 encryption service used to protect sensitive configuration data
/// </summary>
public class EncryptionServiceTests
{
    [Fact]
    public void Encrypt_WithValidPlaintext_ShouldReturnEncryptedBytes()
    {
        // Arrange
        var plaintext = "my-secret-api-key-12345";

        // Act
        var encrypted = EncryptionService.Encrypt(plaintext);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plaintext, Convert.ToBase64String(encrypted));
    }

    [Fact]
    public void Decrypt_WithValidCiphertext_ShouldReturnOriginalPlaintext()
    {
        // Arrange
        var plaintext = "my-secret-api-key-12345";
        var encrypted = EncryptionService.Encrypt(plaintext);

        // Act
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalText = "sk-proj-abc123def456ghi789";

        // Act - Encrypt then decrypt
        var encrypted = EncryptionService.Encrypt(originalText);
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(originalText, decrypted);
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("with spaces and special chars !@#$%")]
    [InlineData("very-long-api-key-that-has-many-characters-to-test-encryption-with-longer-strings-1234567890")]
    [InlineData("unicode-characters-مرحبا-世界-🔐")]
    [InlineData("newlines\nand\ttabs")]
    public void EncryptDecrypt_WithVariousInputs_ShouldPreserveData(string input)
    {
        // Act
        var encrypted = EncryptionService.Encrypt(input);
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(input, decrypted);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ShouldReturnEmptyArray()
    {
        // Arrange
        var plaintext = string.Empty;

        // Act
        var encrypted = EncryptionService.Encrypt(plaintext);

        // Assert
        Assert.NotNull(encrypted);
        Assert.Empty(encrypted);
    }

    [Fact]
    public void Encrypt_WithNull_ShouldReturnEmptyArray()
    {
        // Arrange
        string? plaintext = null;

        // Act
        var encrypted = EncryptionService.Encrypt(plaintext!);

        // Assert
        Assert.NotNull(encrypted);
        Assert.Empty(encrypted);
    }

    [Fact]
    public void Decrypt_WithEmptyArray_ShouldReturnEmptyString()
    {
        // Arrange
        var ciphertext = Array.Empty<byte>();

        // Act
        var decrypted = EncryptionService.Decrypt(ciphertext);

        // Assert
        Assert.Equal(string.Empty, decrypted);
    }

    [Fact]
    public void Decrypt_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        byte[]? ciphertext = null;

        // Act
        var decrypted = EncryptionService.Decrypt(ciphertext!);

        // Assert
        Assert.Equal(string.Empty, decrypted);
    }

    [Fact]
    public void Encrypt_SameInputTwice_ShouldProduceSameOutput()
    {
        // Arrange
        var plaintext = "test-api-key";

        // Act
        var encrypted1 = EncryptionService.Encrypt(plaintext);
        var encrypted2 = EncryptionService.Encrypt(plaintext);

        // Assert
        // Note: Using zero IV means same input produces same output
        // This is a design choice for simplicity - not ideal for production
        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void Encrypt_DifferentInputs_ShouldProduceDifferentOutputs()
    {
        // Arrange
        var plaintext1 = "api-key-1";
        var plaintext2 = "api-key-2";

        // Act
        var encrypted1 = EncryptionService.Encrypt(plaintext1);
        var encrypted2 = EncryptionService.Encrypt(plaintext2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_WithCorruptedData_ShouldThrowCryptographicException()
    {
        // Arrange
        var validEncrypted = EncryptionService.Encrypt("test");
        var corrupted = new byte[validEncrypted.Length];
        Array.Copy(validEncrypted, corrupted, validEncrypted.Length);
        corrupted[0] ^= 0xFF; // Flip all bits in first byte

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => EncryptionService.Decrypt(corrupted));
    }

    [Fact]
    public void Decrypt_WithRandomBytes_ShouldThrowCryptographicException()
    {
        // Arrange
        var random = new Random(12345);
        var randomBytes = new byte[32];
        random.NextBytes(randomBytes);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => EncryptionService.Decrypt(randomBytes));
    }

    [Fact]
    public void Encrypt_WithLargeText_ShouldEncryptSuccessfully()
    {
        // Arrange - Create a large text (10KB)
        var largeText = string.Join("", Enumerable.Repeat("A", 10000));

        // Act
        var encrypted = EncryptionService.Encrypt(largeText);
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(largeText, decrypted);
        Assert.True(encrypted.Length > 0);
    }

    [Fact]
    public void Encrypt_OutputLength_ShouldBeMultipleOf16()
    {
        // Arrange - AES block size is 16 bytes (128 bits)
        var plaintext = "test";

        // Act
        var encrypted = EncryptionService.Encrypt(plaintext);

        // Assert - PKCS7 padding ensures output is multiple of block size
        Assert.Equal(0, encrypted.Length % 16);
    }

    [Fact]
    public void Encrypt_ApiKeyScenario_ShouldWorkCorrectly()
    {
        // Arrange - Realistic API key format
        var openAiKey = "sk-proj-abcdefghijklmnopqrstuvwxyz1234567890";
        var anthropicKey = "sk-ant-api03-abcdefghijklmnopqrstuvwxyz";
        var geminiKey = "AIzaSyAbCdEfGhIjKlMnOpQrStUvWxYz0123456";

        // Act
        var encryptedOpenAi = EncryptionService.Encrypt(openAiKey);
        var encryptedAnthropic = EncryptionService.Encrypt(anthropicKey);
        var encryptedGemini = EncryptionService.Encrypt(geminiKey);

        var decryptedOpenAi = EncryptionService.Decrypt(encryptedOpenAi);
        var decryptedAnthropic = EncryptionService.Decrypt(encryptedAnthropic);
        var decryptedGemini = EncryptionService.Decrypt(encryptedGemini);

        // Assert
        Assert.Equal(openAiKey, decryptedOpenAi);
        Assert.Equal(anthropicKey, decryptedAnthropic);
        Assert.Equal(geminiKey, decryptedGemini);
    }

    [Fact]
    public void Encrypt_ConnectionStringScenario_ShouldWorkCorrectly()
    {
        // Arrange - Database connection string
        var connectionString = "Host=localhost;Port=5432;Database=mydb;Username=admin;Password=secret123!";

        // Act
        var encrypted = EncryptionService.Encrypt(connectionString);
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(connectionString, decrypted);
    }

    [Fact]
    public void Encrypt_WithSingleCharacter_ShouldWorkCorrectly()
    {
        // Arrange
        var plaintext = "X";

        // Act
        var encrypted = EncryptionService.Encrypt(plaintext);
        var decrypted = EncryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plaintext, decrypted);
        Assert.True(encrypted.Length >= 16); // At least one block
    }

    [Fact]
    public void Encrypt_Deterministic_WithSameInput_ProducesSameOutput()
    {
        // Arrange
        var plaintext = "consistent-api-key";

        // Act - Encrypt multiple times
        var results = Enumerable.Range(0, 5)
            .Select(_ => EncryptionService.Encrypt(plaintext))
            .ToList();

        // Assert - All results should be identical (due to zero IV)
        for (int i = 1; i < results.Count; i++)
        {
            Assert.Equal(results[0], results[i]);
        }
    }

    [Fact]
    public void Decrypt_AfterMultipleEncryptions_ShouldStillWork()
    {
        // Arrange
        var originalText = "test-encryption-decryption";

        // Act - Encrypt and decrypt multiple times
        var encrypted1 = EncryptionService.Encrypt(originalText);
        var decrypted1 = EncryptionService.Decrypt(encrypted1);

        var encrypted2 = EncryptionService.Encrypt(decrypted1);
        var decrypted2 = EncryptionService.Decrypt(encrypted2);

        var encrypted3 = EncryptionService.Encrypt(decrypted2);
        var decrypted3 = EncryptionService.Decrypt(encrypted3);

        // Assert
        Assert.Equal(originalText, decrypted1);
        Assert.Equal(originalText, decrypted2);
        Assert.Equal(originalText, decrypted3);
    }
}
