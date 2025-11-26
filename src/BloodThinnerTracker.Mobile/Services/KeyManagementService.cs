using System;
using System.Security.Cryptography;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Basic key derivation and key-management utilities for mobile encrypted storage.
/// Implements PBKDF2 derivation; intended as a starting point for full KDF/HKDF support.
/// </summary>
public class KeyManagementService
{
    public const int DefaultIterations = 100_000;
    public const int KeySizeBytes = 32; // 256-bit

    public byte[] DeriveKey(string password, byte[] salt, int iterations = DefaultIterations)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (salt == null) throw new ArgumentNullException(nameof(salt));
        // Implement PBKDF2-HMAC-SHA256 manually to avoid obsolete API usage and overload ambiguity.
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        try
        {
            return Pbkdf2HmacSha256(passwordBytes, salt, iterations, KeySizeBytes);
        }
        finally
        {
            // Clear sensitive material from memory
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }
    }

    private static byte[] Pbkdf2HmacSha256(byte[] password, byte[] salt, int iterations, int derivedKeyLength)
    {
        using var hmac = new HMACSHA256(password);
        int hashLen = hmac.HashSize / 8;
        int blocks = (derivedKeyLength + hashLen - 1) / hashLen;

        var derived = new byte[derivedKeyLength];
        var block = new byte[hashLen];

        for (int i = 1; i <= blocks; i++)
        {
            // Compute U1 = HMAC(password, salt || INT(i))
            var intBlock = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(i));
            hmac.Initialize();
            hmac.TransformBlock(salt, 0, salt.Length, null, 0);
            hmac.TransformFinalBlock(intBlock, 0, intBlock.Length);
            var u = hmac.Hash!;

            Buffer.BlockCopy(u, 0, block, 0, hashLen);

            var t = new byte[hashLen];
            Array.Copy(u, t, hashLen);

            for (int j = 1; j < iterations; j++)
            {
                u = hmac.ComputeHash(u);
                for (int k = 0; k < hashLen; k++)
                {
                    t[k] ^= u[k];
                }
            }

            int destPos = (i - 1) * hashLen;
            int bytesToCopy = Math.Min(hashLen, derivedKeyLength - destPos);
            Array.Copy(t, 0, derived, destPos, bytesToCopy);
        }

        // Clear intermediate buffers
        Array.Clear(block, 0, block.Length);
        return derived;
    }

    public byte[] GenerateSalt(int size = 16)
    {
        var salt = new byte[size];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    // Placeholder for key rotation logic; in a production app this would store versioned keys,
    // support re-encrypting cached data and provide atomic rotation.
    public void RotateKey()
    {
        // TODO: Implement rotation and migration support
        throw new NotImplementedException();
    }
}
