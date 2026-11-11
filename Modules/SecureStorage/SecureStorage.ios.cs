
#if ENABLE_SECURE_STORAGE && UNITY_IOS && !UNITY_EDITOR

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Modules.SecureStorage
{
    public static partial class SecureStorage
    {
        /// <summary> Keychain の汎用パスワード項目に保存する（この端末だけ・初回ロック解除後に読み書きできる） </summary>
        private sealed class KeychainStorage : IStorage
        {
            //----- params -----

            // errSecSuccess.
            private const int StatusSuccess = 0;

            // errSecItemNotFound.
            private const int StatusItemNotFound = -25300;

            //----- field -----

            //----- property -----

            //----- method -----

            [DllImport("__Internal")]
            private static extern int _SecureStorage_Set(byte[] service, int serviceLength, byte[] key, int keyLength, byte[] value, int valueLength);

            [DllImport("__Internal")]
            private static extern string _SecureStorage_Get(byte[] service, int serviceLength, byte[] key, int keyLength, out int status);

            [DllImport("__Internal")]
            private static extern int _SecureStorage_Delete(byte[] service, int serviceLength, byte[] key, int keyLength);

            [DllImport("__Internal")]
            private static extern int _SecureStorage_DeleteAll(byte[] service, int serviceLength);

            public bool Set(string service, string key, byte[] value)
            {
                var serviceBytes = Encoding.UTF8.GetBytes(service);
                var keyBytes = Encoding.UTF8.GetBytes(key);

                var status = _SecureStorage_Set(serviceBytes, serviceBytes.Length, keyBytes, keyBytes.Length, value, value.Length);

                if (status != StatusSuccess)
                {
                    Debug.LogError($"SecureStorage set failed. (key = {key}, status = {status})");

                    return false;
                }

                return true;
            }

            public bool TryGet(string service, string key, out byte[] value)
            {
                value = null;

                var serviceBytes = Encoding.UTF8.GetBytes(service);
                var keyBytes = Encoding.UTF8.GetBytes(key);

                var status = 0;

                var base64 = _SecureStorage_Get(serviceBytes, serviceBytes.Length, keyBytes, keyBytes.Length, out status);

                if (status == StatusItemNotFound){ return false; }

                if (status != StatusSuccess || base64 == null)
                {
                    Debug.LogError($"SecureStorage get failed. (key = {key}, status = {status})");

                    return false;
                }

                value = Convert.FromBase64String(base64);

                return true;
            }

            public bool Delete(string service, string key)
            {
                var serviceBytes = Encoding.UTF8.GetBytes(service);
                var keyBytes = Encoding.UTF8.GetBytes(key);

                var status = _SecureStorage_Delete(serviceBytes, serviceBytes.Length, keyBytes, keyBytes.Length);

                if (status != StatusSuccess && status != StatusItemNotFound)
                {
                    Debug.LogError($"SecureStorage delete failed. (key = {key}, status = {status})");

                    return false;
                }

                return true;
            }

            public bool DeleteAll(string service)
            {
                var serviceBytes = Encoding.UTF8.GetBytes(service);

                var status = _SecureStorage_DeleteAll(serviceBytes, serviceBytes.Length);

                if (status != StatusSuccess && status != StatusItemNotFound)
                {
                    Debug.LogError($"SecureStorage delete all failed. (service = {service}, status = {status})");

                    return false;
                }

                return true;
            }
        }
    }
}

#endif
