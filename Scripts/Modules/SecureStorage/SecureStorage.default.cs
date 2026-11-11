
#if ENABLE_SECURE_STORAGE && (UNITY_EDITOR || !(UNITY_IOS || UNITY_ANDROID))

using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.SecureStorage
{
    public static partial class SecureStorage
    {
        /// <summary> 安全な保存領域が無い環境で SecurePrefs に保存する </summary>
        private sealed class PrefsStorage : IStorage
        {
            //----- params -----

            private const string KeyListName = "KeyList";

            //----- field -----

            //----- property -----

            //----- method -----

            public PrefsStorage()
            {
                #if !UNITY_EDITOR

                Debug.LogWarning("SecureStorage is not supported on this platform. Values are saved to SecurePrefs.");

                #endif
            }

            public bool Set(string service, string key, byte[] value)
            {
                SecurePrefs.SetString(GetPrefsKey(service, key), Convert.ToBase64String(value));

                var keys = GetKeyList(service);

                if (!keys.Contains(key))
                {
                    keys.Add(key);

                    SetKeyList(service, keys);
                }

                SecurePrefs.Save();

                return true;
            }

            public bool TryGet(string service, string key, out byte[] value)
            {
                value = null;

                var prefsKey = GetPrefsKey(service, key);

                if (!SecurePrefs.HasKey(prefsKey)){ return false; }

                try
                {
                    var base64 = SecurePrefs.GetString(prefsKey);

                    // 長さ 0 の値は空文字で保存され、読み込み時は null か空文字になる.
                    value = string.IsNullOrEmpty(base64) ? new byte[0] : Convert.FromBase64String(base64);
                }
                catch (FormatException e)
                {
                    Debug.LogError($"SecureStorage get failed. (key = {key})\n{e}");

                    return false;
                }

                return true;
            }

            public bool Delete(string service, string key)
            {
                SecurePrefs.DeleteKey(GetPrefsKey(service, key));

                var keys = GetKeyList(service);

                if (keys.Remove(key))
                {
                    SetKeyList(service, keys);
                }

                SecurePrefs.Save();

                return true;
            }

            public bool DeleteAll(string service)
            {
                var keys = GetKeyList(service);

                foreach (var key in keys)
                {
                    SecurePrefs.DeleteKey(GetPrefsKey(service, key));
                }

                SecurePrefs.DeleteKey(GetKeyListPrefsKey(service));

                SecurePrefs.Save();

                return true;
            }

            private static string GetPrefsKey(string service, string key)
            {
                return $"{typeof(PrefsStorage).FullName}-{service}-Value-{key}";
            }

            private static string GetKeyListPrefsKey(string service)
            {
                return $"{typeof(PrefsStorage).FullName}-{service}-{KeyListName}";
            }

            private static List<string> GetKeyList(string service)
            {
                var keys = SecurePrefs.Get<List<string>>(GetKeyListPrefsKey(service));

                return keys != null ? keys : new List<string>();
            }

            private static void SetKeyList(string service, List<string> keys)
            {
                SecurePrefs.Set(GetKeyListPrefsKey(service), keys);
            }
        }
    }
}

#endif
