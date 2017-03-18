using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KSS
{
    public class DownloadLinkCrypto
    {
        /*Deobfuscated from vr.js
        var bkZ = 'a5e8d2e9c1721ae0e84ad660c472c1f3',//_0x59b3[31],
        skH = 'nhasasdbasdtene7230asb',//_0x59b3[32],
        iv, key;
        iv = CryptoJS.enc.Hex.parse(bkZ);
        key = CryptoJS.SHA256(skH);
        */
        private Aes _aes = Aes.Create();
        public DownloadLinkCrypto() : this("a5e8d2e9c1721ae0e84ad660c472c1f3", "nhasasdbasdtene7230asb") { }

        public DownloadLinkCrypto(string IV, string key)
        {
            _aes.BlockSize = 128;
            _aes.KeySize = 128;
            _aes.Padding = PaddingMode.PKCS7;
            _aes.Mode = CipherMode.CBC;

            _aes.IV = HexStringToByteArray(IV);

            using (SHA256 sha = SHA256.Create())
            {
                _aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }

        public string Decrypt(string ciperText, Func<string,object> log)
        {
            byte[] ciperTextBytes = Convert.FromBase64String(ciperText);
            //byte[] ciperTextBytes = Convert.FromBase64String(Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(ciperText)));
            ICryptoTransform cr = _aes.CreateDecryptor();
            //log(ciperTextBytes.Length.ToString());
            byte[] decryptedTextBytes = cr.TransformFinalBlock(ciperTextBytes, 0, ciperTextBytes.Length);

            return Encoding.UTF8.GetString(decryptedTextBytes);
        }

        public string Encrypt(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            ICryptoTransform cr = _aes.CreateEncryptor();
            byte[] ciperTextBytes = cr.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);

            return Convert.ToBase64String(ciperTextBytes);
        }


        public static byte[] HexStringToByteArray(string strHex)
        {
            // dynamic r = new byte[strHex.Length / 2];
            // for (int i = 0; i <= strHex.Length - 1; i += 2)
            // {
            //     r[i / 2] = Convert.ToByte(Convert.ToInt32(strHex.Substring(i, 2), 16));
            // }
            // return r;
            return Enumerable.Range(0, strHex.Length / 2) .Select(x => Convert.ToByte(strHex.Substring(x * 2, 2), 16)) .ToArray();
        }
    }
}
