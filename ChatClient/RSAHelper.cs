using System;
using System.Security.Cryptography;
using System.Text;

public static class RSAHelper
{
    public static string Encrypt(string plainText, string keyB64)
    {
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        using RSA rsa = RSA.Create();

        try
        {
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keyB64), out _);
        }
        catch
        {
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(keyB64), out _);
        }

        byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string encryptedB64, string privateKeyB64)
    {
        byte[] data = Convert.FromBase64String(encryptedB64);
        using RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyB64), out _);
        byte[] decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(decrypted);
    }
}
