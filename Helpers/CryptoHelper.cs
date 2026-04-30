namespace SecureTcpServer.Helpers;

using System.Security.Cryptography;
using System.Text;

public class CryptoHelper
{
    private byte[] _key;
    private byte[] _iv;
    public CryptoHelper(string Key, string Iv) 
    {
        _key = Encoding.UTF8.GetBytes(Key);
        _iv = Encoding.UTF8.GetBytes(Iv);
    }

    public byte[] Encrypt(string text)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key; aes.IV = _iv;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        byte[] data = Encoding.UTF8.GetBytes(text);
        cs.Write(data); cs.FlushFinalBlock();
        return ms.ToArray();
    }

    public string Decrypt(byte[] encrypted)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key; aes.IV = _iv;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using MemoryStream ms = new MemoryStream(encrypted);
        using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}