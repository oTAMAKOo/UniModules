
#if ENABLE_DEVICE_KEY && UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using System;

namespace Modules.DeviceKey
{
    public static partial class DeviceKey
    {
        /// <summary> AndroidKeyStore に鍵を作る </summary>
        private sealed class AndroidKeyStore : IKeyStore
        {
            //----- params -----

            private const string JavaClassName = "com.unimodules.devicekey.DeviceKey";

            //----- field -----

            private AndroidJavaClass javaClass = null;

            //----- property -----

            public bool IsSupported { get { return true; } }

            //----- method -----

            public AndroidKeyStore()
            {
                javaClass = new AndroidJavaClass(JavaClassName);
            }

            public bool HasKey(string alias)
            {
                return javaClass.CallStatic<bool>("hasKey", alias);
            }

            public bool CreateKey(string alias)
            {
                var result = javaClass.CallStatic<bool>("createKey", alias);

                if (!result)
                {
                    Debug.LogError($"DeviceKey create key failed. (alias = {alias})");
                }

                return result;
            }

            public byte[] GetPublicKey(string alias)
            {
                // 鍵が無い・失敗したときは null が返る.
                using (var result = javaClass.CallStatic<AndroidJavaObject>("getPublicKey", alias))
                {
                    if (result == null){ return null; }

                    return Convert.FromBase64String(result.Call<string>("toString"));
                }
            }

            public byte[] Sign(string alias, byte[] data)
            {
                // 鍵が無い・失敗したときは null が返る.
                using (var result = javaClass.CallStatic<AndroidJavaObject>("sign", alias, Convert.ToBase64String(data)))
                {
                    if (result == null){ return null; }

                    return Convert.FromBase64String(result.Call<string>("toString"));
                }
            }

            public bool DeleteKey(string alias)
            {
                var result = javaClass.CallStatic<bool>("deleteKey", alias);

                if (!result)
                {
                    Debug.LogError($"DeviceKey delete key failed. (alias = {alias})");
                }

                return result;
            }
        }
    }
}

#endif
