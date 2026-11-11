
#if ENABLE_SECURE_STORAGE && UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using System;

namespace Modules.SecureStorage
{
    public static partial class SecureStorage
    {
        /// <summary> Keystore の鍵で暗号化し、バックアップ対象外の領域にファイルで保存する </summary>
        private sealed class KeystoreStorage : IStorage
        {
            //----- params -----

            private const string JavaClassName = "com.unimodules.securestorage.SecureStorage";

            //----- field -----

            private AndroidJavaClass javaClass = null;

            private AndroidJavaObject context = null;

            //----- property -----

            //----- method -----

            public KeystoreStorage()
            {
                javaClass = new AndroidJavaClass(JavaClassName);

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        context = activity.Call<AndroidJavaObject>("getApplicationContext");
                    }
                }
            }

            public bool Set(string service, string key, byte[] value)
            {
                var result = javaClass.CallStatic<bool>("set", context, service, key, Convert.ToBase64String(value));

                if (!result)
                {
                    Debug.LogError($"SecureStorage set failed. (key = {key})");
                }

                return result;
            }

            public bool TryGet(string service, string key, out byte[] value)
            {
                value = null;

                // 値が無い・読めないときは null が返る.
                using (var result = javaClass.CallStatic<AndroidJavaObject>("get", context, service, key))
                {
                    if (result == null){ return false; }

                    value = Convert.FromBase64String(result.Call<string>("toString"));
                }

                return true;
            }

            public bool Delete(string service, string key)
            {
                var result = javaClass.CallStatic<bool>("delete", context, service, key);

                if (!result)
                {
                    Debug.LogError($"SecureStorage delete failed. (key = {key})");
                }

                return result;
            }

            public bool DeleteAll(string service)
            {
                var result = javaClass.CallStatic<bool>("deleteAll", context, service);

                if (!result)
                {
                    Debug.LogError($"SecureStorage delete all failed. (service = {service})");
                }

                return result;
            }
        }
    }
}

#endif
