using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public class EncryptedPayload
    {
        public string CiphertextBase64 { get; set; } = string.Empty;
        public string IvBase64 { get; set; } = string.Empty;
        public string TagBase64 { get; set; } = string.Empty;
    }

    public class EncryptionService
    {
        public byte[] GenerateRandomKey()
        {
            var key = new byte[32]; // 256-bit
            RandomNumberGenerator.Fill(key);
            return key;
        }

        public EncryptedPayload Encrypt(byte[] key, string plainText)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            var iv = new byte[12];
            RandomNumberGenerator.Fill(iv);
            var tag = new byte[16];
            var ciphertext = new byte[plaintextBytes.Length];

            using (var aes = new AesGcm(key, 16))
            {
                aes.Encrypt(iv, plaintextBytes, ciphertext, tag);
            }

            return new EncryptedPayload
            {
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
                IvBase64 = Convert.ToBase64String(iv),
                TagBase64 = Convert.ToBase64String(tag)
            };
        }

        public string Decrypt(byte[] key, EncryptedPayload payload)
        {
            var ciphertext = Convert.FromBase64String(payload.CiphertextBase64);
            var iv = Convert.FromBase64String(payload.IvBase64);
            var tag = Convert.FromBase64String(payload.TagBase64);
            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(key, 16))
            {
                aes.Decrypt(iv, ciphertext, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
