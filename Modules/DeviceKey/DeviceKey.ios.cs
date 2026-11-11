
#if ENABLE_DEVICE_KEY && UNITY_IOS && !UNITY_EDITOR

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Modules.DeviceKey
{
    public static partial class DeviceKey
    {
        /// <summary> Secure Enclave に鍵を作る（この端末だけ・初回ロック解除後に使える） </summary>
        private sealed class SecureEnclaveKeyStore : IKeyStore
        {
            //----- params -----

            // errSecSuccess.
            private const int StatusSuccess = 0;

            // errSecItemNotFound.
            private const int StatusItemNotFound = -25300;

            // P-256 の公開鍵（X9.63 非圧縮 65 バイト）を SubjectPublicKeyInfo にする前置き.
            private static readonly byte[] SubjectPublicKeyInfoPrefix = new byte[]
            {
                0x30, 0x59, 0x30, 0x13, 0x06, 0x07, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01,
                0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07, 0x03, 0x42, 0x00,
            };

            private const int RawPublicKeyLength = 65;

            //----- field -----

            //----- property -----

            public bool IsSupported { get { return true; } }

            //----- method -----

            [DllImport("__Internal")]
            private static extern int _DeviceKey_Has(byte[] tag, int tagLength);

            [DllImport("__Internal")]
            private static extern int _DeviceKey_Create(byte[] tag, int tagLength);

            [DllImport("__Internal")]
            private static extern string _DeviceKey_GetPublicKey(byte[] tag, int tagLength, out int status);

            [DllImport("__Internal")]
            private static extern string _DeviceKey_Sign(byte[] tag, int tagLength, byte[] data, int dataLength, out int status);

            [DllImport("__Internal")]
            private static extern int _DeviceKey_Delete(byte[] tag, int tagLength);

            public bool HasKey(string alias)
            {
                var tag = Encoding.UTF8.GetBytes(alias);

                var status = _DeviceKey_Has(tag, tag.Length);

                if (status == StatusItemNotFound){ return false; }

                if (status != StatusSuccess)
                {
                    Debug.LogError($"DeviceKey has key failed. (alias = {alias}, status = {status})");

                    return false;
                }

                return true;
            }

            public bool CreateKey(string alias)
            {
                var tag = Encoding.UTF8.GetBytes(alias);

                var status = _DeviceKey_Create(tag, tag.Length);

                if (status != StatusSuccess)
                {
                    Debug.LogError($"DeviceKey create key failed. (alias = {alias}, status = {status})");

                    return false;
                }

                return true;
            }

            public byte[] GetPublicKey(string alias)
            {
                var tag = Encoding.UTF8.GetBytes(alias);

                var status = 0;

                var base64 = _DeviceKey_GetPublicKey(tag, tag.Length, out status);

                if (status == StatusItemNotFound){ return null; }

                if (status != StatusSuccess || base64 == null)
                {
                    Debug.LogError($"DeviceKey get public key failed. (alias = {alias}, status = {status})");

                    return null;
                }

                var rawPublicKey = Convert.FromBase64String(base64);

                if (rawPublicKey.Length != RawPublicKeyLength)
                {
                    Debug.LogError($"DeviceKey public key length is invalid. (alias = {alias}, length = {rawPublicKey.Length})");

                    return null;
                }

                var publicKey = new byte[SubjectPublicKeyInfoPrefix.Length + rawPublicKey.Length];

                Buffer.BlockCopy(SubjectPublicKeyInfoPrefix, 0, publicKey, 0, SubjectPublicKeyInfoPrefix.Length);
                Buffer.BlockCopy(rawPublicKey, 0, publicKey, SubjectPublicKeyInfoPrefix.Length, rawPublicKey.Length);

                return publicKey;
            }

            public byte[] Sign(string alias, byte[] data)
            {
                var tag = Encoding.UTF8.GetBytes(alias);

                var status = 0;

                var base64 = _DeviceKey_Sign(tag, tag.Length, data, data.Length, out status);

                if (status == StatusItemNotFound){ return null; }

                if (status != StatusSuccess || base64 == null)
                {
                    Debug.LogError($"DeviceKey sign failed. (alias = {alias}, status = {status})");

                    return null;
                }

                return Convert.FromBase64String(base64);
            }

            public bool DeleteKey(string alias)
            {
                var tag = Encoding.UTF8.GetBytes(alias);

                var status = _DeviceKey_Delete(tag, tag.Length);

                if (status != StatusSuccess && status != StatusItemNotFound)
                {
                    Debug.LogError($"DeviceKey delete key failed. (alias = {alias}, status = {status})");

                    return false;
                }

                return true;
            }
        }
    }
}

#endif
