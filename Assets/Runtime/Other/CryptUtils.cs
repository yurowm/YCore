using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Yurowm.Extensions;

namespace Yurowm.Utilities {
    public static class CryptUtils {
        
        static readonly CryptKey defaultKey = CryptKey.New(
            Convert.FromBase64String(@"4Nt+UZaYnFvYUXSUmb5CB6cplSa8uyT/uXsEGy5vNDU="),
            Convert.FromBase64String(@"23KwxIzd3IAi0tuGr9qh5w=="));

        public static string Encrypt(this string text) {
            return Encrypt(text, defaultKey);
        }
        
        public static string Encrypt(this string text, string key) {
            return Encrypt(text, CryptKey.Get(key));
        }
        
        public static string Encrypt(this string text, CryptKey key) {
            return Encrypt(text, key.Key, key.IV);
        }
            
        public static string Encrypt(this string text, byte[] Key, byte[] IV) {
            if (text.IsNullOrEmpty()) return "";
            return Convert.ToBase64String(EncryptStringToBytesAes(text, Key, IV));
        }
        
        public static string Decrypt(this string encrypted) {
            return Decrypt(encrypted, defaultKey);
        }
        
        public static string Decrypt(this string encrypted, string key) {
            return Decrypt(encrypted, CryptKey.Get(key));
        }
        
        public static string Decrypt(this string encrypted, CryptKey key) {
            return Decrypt(encrypted, key.Key, key.IV);
        }

        public static string Decrypt(this string encrypted, byte[] Key, byte[] IV) {
            try {
                return DecryptStringFromBytesAes(Convert.FromBase64String(encrypted), Key, IV);
            } catch (Exception e) {
                return "";
            }
        }
        
        static byte[] EncryptStringToBytesAes(string plainText, byte[] Key, byte[] IV) {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            byte[] encrypted;
 
            using (Aes aesAlg = Aes.Create()) {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
 
                using (var msEncrypt = new MemoryStream()) {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                            swEncrypt.Write(plainText);
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            
            return encrypted;
        }
                
        static string DecryptStringFromBytesAes(byte[] cipherText, byte[] Key, byte[] IV) {
            
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));
            
            string plaintext;
 
            using (Aes aesAlg = Aes.Create()) {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                
                using (var msDecrypt = new MemoryStream(cipherText))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (var srDecrypt = new StreamReader(csDecrypt))
                            plaintext = srDecrypt.ReadToEnd();
            }
 
            return plaintext;
        }

    }
    
    public class CryptKey {
        static readonly Dictionary<string, CryptKey> keyStore = new Dictionary<string, CryptKey>();
        
        public readonly byte[] Key;
        public readonly byte[] IV;

        CryptKey(byte[] key, byte[] iv) {
            Key = key;
            IV = iv;
        }
        
        public static CryptKey New(byte[] key, byte[] iv) {
            return new CryptKey(key, iv);
        }
        
        public static CryptKey Get(string key) {
            if (keyStore.TryGetValue(key, out var result))
                return result;

            TextToKey(key, out var _key, out var _iv);
            result = new CryptKey(_key, _iv);
            keyStore.Add(key, result);
            
            return result;
        }
        
        static void TextToKey(string text, out byte[] key, out byte[] iv) {
            var generator = ByteGenerator(text);
            
            key = Enumerator.For(1, 32, 1).Select(i => {
                generator.MoveNext();
                return generator.Current;
            }).ToArray();            

            iv = Enumerator.For(1, 16, 1).Select(i => {
                generator.MoveNext();
                return generator.Current;
            }).ToArray();
        }
        
        static IEnumerator<byte> ByteGenerator(string key) {
            const long m = 32768;
            const long a = 1103515245;
            const long b = 65530;
            const long c = 12345;

            char[] chars = key.ToCharArray();
            int count = 16 + 32;
            
            long result = c;
            for (int index = 0; index < count; index++) {
                long current = index < chars.Length ? Convert.ToInt32(chars[index]) : 1;
                result = (a * result * current / b + c) % m;
                yield return (byte) (result % 256);
            }
        }
    }
}