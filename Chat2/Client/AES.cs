using System;

public class Class1
{
    public Class1()
    { }
    private static readonly byte[] _Salt = new byte[] { 1, 2, 23, 234, 37, 48, 134, 63, 248, 4 };
    private const int KEY_SIZE = 256;
    private const int BLOCK_SIZE = 128;
    private const PaddingMode PADDING_MODE = PaddingMode.PKCS7;
    private static readonly byte[] _AESKey = GetEncryptionKey(KEY_SIZE);

    public static string Encrypt(string dataToEncrypt)
    {
        if (dataToEncrypt == null || dataToEncrypt.Length < 1)
        {
            throw new ArgumentNullException("dataToEncrypt");
        }

        var data = Encoding.UTF8.GetBytes(dataToEncrypt);
        var encrypted = Encrypt(data);

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string dataToDecrypt)
    {
        if (dataToDecrypt == null || dataToDecrypt.Length < 1)
        {
            throw new ArgumentNullException("dataToDecrypt");
        }

        var data = Convert.FromBase64String(dataToDecrypt);
        var decrypted = Decrypt(data);

        return Encoding.UTF8.GetString(decrypted);
    }

    public static byte[] Encrypt(byte[] dataToEncrypt)
    {
        if (dataToEncrypt == null || dataToEncrypt.Length < 1)
        {
            throw new ArgumentNullException("dataToEncrypt");
        }

        return Encrypt(
            dataToEncrypt,
            _AESKey
            );
    }

    public static byte[] Decrypt(byte[] dataToDecrypt)
    {
        if (dataToDecrypt == null || dataToDecrypt.Length < 1)
        {
            throw new ArgumentNullException("dataToDecrypt");
        }

        return Decrypt(
            dataToDecrypt,
            _AESKey
            );
    }

    private static byte[] Encrypt(byte[] dataToEncrypt, byte[] key)
    {
        byte[] encryptedData;

        using (var rij = new RijndaelManaged())
        {
            rij.KeySize = KEY_SIZE;
            rij.BlockSize = BLOCK_SIZE;
            rij.Padding = PADDING_MODE;
            rij.Key = key;
            rij.GenerateIV();

            ICryptoTransform encryptor = rij.CreateEncryptor(rij.Key, rij.IV);

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        binaryWriter.Write(rij.IV);
                        binaryWriter.Write(dataToEncrypt);
                        cryptoStream.Flush();
                        cryptoStream.FlushFinalBlock();

                        memoryStream.Position = 0;
                        encryptedData = memoryStream.ToArray();
                    }



                    memoryStream.Close();
                    cryptoStream.Close();
                }
            }
        }

        return encryptedData;
    }

    private static byte[] Decrypt(byte[] dataToDecrypt, byte[] key)
    {
        byte[] decrypted;

        using (var rij = new RijndaelManaged())
        {
            rij.KeySize = KEY_SIZE;
            rij.BlockSize = BLOCK_SIZE;
            rij.Padding = PADDING_MODE;
            rij.Key = key;
            var iv = new byte[16];
            Array.Copy(dataToDecrypt, 0, iv, 0, iv.Length);
            rij.IV = iv;

            ICryptoTransform decryptor = rij.CreateDecryptor(rij.Key, rij.IV);

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        binaryWriter.Write(dataToDecrypt, iv.Length, dataToDecrypt.Length - iv.Length);
                        cryptoStream.Flush();
                        cryptoStream.FlushFinalBlock();
                        memoryStream.Position = 0;
                    }

                    decrypted = memoryStream.ToArray();
                    memoryStream.Close();
                    cryptoStream.Close();
                }
            }
        }

        return decrypted;
    }

    private static byte[] GenerateIV()
    {
        byte[] iv;

        using (var aes = new AesCryptoServiceProvider())
        {
            aes.GenerateIV();
            iv = aes.IV;
        }

        return iv;
    }

    private static byte[] GetEncryptionKey(int keySize)
    {
        byte[] key;
        var passphrase = @"some passphrase retrieved from server";

        var encryptedPassphrase = RSA.Encrypt(passphrase);
        var encryptedPassphraseBytes = new byte[encryptedPassphrase.Length * sizeof(char)];
        System.Buffer.BlockCopy(encryptedPassphrase.ToCharArray(), 0, encryptedPassphraseBytes, 0, encryptedPassphraseBytes.Length);

        return encryptedPassphraseBytes.Take(keySize / 8).ToArray();
    }
}

