using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Harmony {
    public static class Crypto {
        private const int Keysize = 256;
        private const int DerivationIterations = 100_000;

        private static byte[] derivedPass;

        internal static void Init(byte[] salt, SecureString passPhrase) {
            derivedPass = new Rfc2898DeriveBytes(SecureStringToString(passPhrase), salt, DerivationIterations).GetBytes(Keysize / 8);
        }

        internal static byte[] Init(SecureString passPhrase) {
            var salt = Generate256BitsOfRandomEntropy();
            derivedPass = new Rfc2898DeriveBytes(SecureStringToString(passPhrase), salt, DerivationIterations).GetBytes(Keysize / 8);
            return salt;
        }

        internal static string Encrypt(string plainText) {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var iv = Generate256BitsOfRandomEntropy();

            using (var rijndaelManaged = new RijndaelManaged()) {
                rijndaelManaged.Padding = PaddingMode.PKCS7;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.BlockSize = 256;

                using (var encryptor = rijndaelManaged.CreateEncryptor(derivedPass, iv)) {
                    using (var ms = new MemoryStream()) {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
                            cs.Write(bytes, 0, bytes.Length);
                            cs.FlushFinalBlock();
                            var encBytes = iv;
                            encBytes = encBytes.Concat(ms.ToArray()).ToArray();
                            return Convert.ToBase64String(encBytes);
                        }
                    }
                }
            }

        }

        internal static string Decrypt(string cipherText) {
            var bytes = Convert.FromBase64String(cipherText);
            var data = bytes.Skip(Keysize / 8).Take(bytes.Length - Keysize / 8).ToArray();
            var iv = bytes.Take(Keysize / 8).ToArray();

            using (var rijndaelManaged = new RijndaelManaged()) {
                rijndaelManaged.BlockSize = 256;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.Padding = PaddingMode.PKCS7;

                using (var decryptor = rijndaelManaged.CreateDecryptor(derivedPass, iv)) {
                    using (var ms = new MemoryStream(data)) {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                            var plainTextBytes = new byte[data.Length];
                            var decryptedByteCount = cs.Read(plainTextBytes, 0, plainTextBytes.Length);
                            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy() {
            var randomBytes = new byte[32];
            using (var rngCsp = new RNGCryptoServiceProvider()) {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        internal static string SecureStringToString(SecureString value) {
            var valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
