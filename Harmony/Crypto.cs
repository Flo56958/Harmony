using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Harmony {
    public static class Crypto {
        private const int Keysize = 128;
        private const int DerivationIterations = 1_000;

        private static byte[] _derivedPass;

        internal static byte[] Init(SecureString passPhrase, byte[] salt = null) {
            salt ??= Generate128BitsOfRandomEntropy();
            _derivedPass = new Rfc2898DeriveBytes(new System.Net.NetworkCredential(string.Empty, passPhrase).Password, salt, DerivationIterations).GetBytes(Keysize / 8);
            return salt;
        }

        internal static byte[] Encrypt(byte[] bytes) {
            var iv = Generate128BitsOfRandomEntropy();

            using var rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Padding = PaddingMode.PKCS7;
            rijndaelManaged.Mode = CipherMode.CBC;
            rijndaelManaged.BlockSize = 128;

            using var encryptor = rijndaelManaged.CreateEncryptor(_derivedPass, iv);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
            return iv.Concat(ms.ToArray()).ToArray();
        }

        internal static byte[] Decrypt(byte[] bytes) {
            var data = bytes.Skip(Keysize / 8).Take(bytes.Length - Keysize / 8).ToArray();
            var iv = bytes.Take(Keysize / 8).ToArray();

            using var rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.BlockSize = 128;
            rijndaelManaged.Mode = CipherMode.CBC;
            rijndaelManaged.Padding = PaddingMode.PKCS7;

            using var decryptor = rijndaelManaged.CreateDecryptor(_derivedPass, iv);
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            var plainTextBytes = new byte[data.Length];
            return plainTextBytes.Take(cs.Read(plainTextBytes, 0, plainTextBytes.Length)).ToArray();
        }

        private static byte[] Generate128BitsOfRandomEntropy() {
            var randomBytes = new byte[16];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}
