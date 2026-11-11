
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class HashExtension
    {
        //----- params -----

        //----- field -----

        #if NET6_0_OR_GREATER

        private static SHA256 hashAlgorithmSHA256 = null;

        #else

        private static SHA256CryptoServiceProvider hashAlgorithmSHA256 = null;

        #endif

        //----- property -----

        //----- method -----

        static HashExtension()
        {
            #if NET6_0_OR_GREATER

            hashAlgorithm = SHA256.Create();

            #else

            hashAlgorithmSHA256 = new SHA256CryptoServiceProvider();

            #endif
        }

        public static string CalcSHA256(FileStream fileStream)
        {
            byte[] bytes = null;
            
            lock (hashAlgorithmSHA256)
            {
                #if NET6_0_OR_GREATER

                if (!hashAlgorithmSHA256.CanReuseTransform)
                {
                    hashAlgorithm = SHA256.Create();
                }

                #else

                if (!hashAlgorithmSHA256.CanReuseTransform)
                {
                    hashAlgorithmSHA256 = new SHA256CryptoServiceProvider();
                }

                #endif

                bytes = hashAlgorithmSHA256.ComputeHash(fileStream);
            }

            var hashedText = new StringBuilder();

            foreach (var b in bytes)
            {
                hashedText.Append(b.ToString("x2"));
            }

            return hashedText.ToString();
        }

        public static string CalcSHA256(string value, Encoding enc)
        {
            var byteValues = enc.GetBytes(value);

            byte[] bytes = null;

            lock (hashAlgorithmSHA256)
            {
                #if NET6_0_OR_GREATER

                if (!hashAlgorithmSHA256.CanReuseTransform)
                {
                    hashAlgorithm = SHA256.Create();
                }

                #else

                if (!hashAlgorithmSHA256.CanReuseTransform)
                {
                    hashAlgorithmSHA256 = new SHA256CryptoServiceProvider();
                }

                #endif

                bytes = hashAlgorithmSHA256.ComputeHash(byteValues);
            }

            return string.Join("", bytes.Select(x => $"{x:x2}"));
        }
    }
}