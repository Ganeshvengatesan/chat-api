namespace ChatApplicationAPI.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText, string? key = null);
    string Decrypt(string cipherText, string? key = null);
}
