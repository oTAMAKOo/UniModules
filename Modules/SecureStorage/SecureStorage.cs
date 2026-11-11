
#if ENABLE_SECURE_STORAGE

using UnityEngine;
using System;
using System.Text;
using Newtonsoft.Json;

namespace Modules.SecureStorage
{
    /// <summary>
    /// OS の安全な保存領域にキー値を保存する.
    /// iOS は Keychain、Android は Keystore の鍵で暗号化したファイル、その他の環境は SecurePrefs に保存する.
    /// メインスレッドから呼ぶ.
    /// </summary>
    public static partial class SecureStorage
    {
        //----- params -----

        private interface IStorage
        {
            bool Set(string service, string key, byte[] value);

            bool TryGet(string service, string key, out byte[] value);

            bool Delete(string service, string key);

            bool DeleteAll(string service);
        }

        private const string DefaultServiceName = "SecureStorage";

        private const string InstalledPrefsKeyPrefix = "SecureStorage-Installed-";

        //----- field -----

        private static string serviceName = DefaultServiceName;

        private static IStorage storage = null;

        //----- property -----

        /// <summary> 保存先の区分名（iOS は Keychain の service、Android は鍵と保存フォルダの区別に使う） </summary>
        public static string ServiceName { get { return serviceName; } }

        //----- method -----

        /// <summary> 保存先の区分名を設定する（読み書きより先に呼ぶ） </summary>
        public static void SetServiceName(string name)
        {
            if (string.IsNullOrEmpty(name)){ throw new ArgumentException("Service name is null or empty."); }

            serviceName = name;
        }

        /// <summary>
        /// インストール後の最初の呼び出しで、この区分名の値をすべて削除する.
        /// iOS の Keychain はアプリを削除しても値が残るため、再インストール後に値を引き継がないときに起動時に呼ぶ.
        /// </summary>
        public static void ClearOnFreshInstall()
        {
            var prefsKey = InstalledPrefsKeyPrefix + serviceName;

            if (PlayerPrefs.HasKey(prefsKey)){ return; }

            // 削除に失敗したときは次の起動で再度削除する.
            if (!DeleteAll()){ return; }

            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
        }

        //====== Utility ======

        public static bool HasKey(string key)
        {
            byte[] value = null;

            return TryGetBytes(key, out value);
        }

        public static bool DeleteKey(string key)
        {
            ValidateKey(key);

            return GetStorage().Delete(serviceName, key);
        }

        /// <summary> この区分名の値をすべて削除する </summary>
        public static bool DeleteAll()
        {
            return GetStorage().DeleteAll(serviceName);
        }

        //====== Bytes ======

        public static bool SetBytes(string key, byte[] value)
        {
            ValidateKey(key);

            if (value == null){ throw new ArgumentNullException("value"); }

            return GetStorage().Set(serviceName, key, value);
        }

        public static bool TryGetBytes(string key, out byte[] value)
        {
            ValidateKey(key);

            return GetStorage().TryGet(serviceName, key, out value);
        }

        public static byte[] GetBytes(string key, byte[] defaultValue = null)
        {
            byte[] value = null;

            return TryGetBytes(key, out value) ? value : defaultValue;
        }

        //====== String ======

        public static bool SetString(string key, string value)
        {
            if (value == null){ throw new ArgumentNullException("value"); }

            return SetBytes(key, Encoding.UTF8.GetBytes(value));
        }

        public static bool TryGetString(string key, out string value)
        {
            value = null;

            byte[] bytes = null;

            if (!TryGetBytes(key, out bytes)){ return false; }

            value = Encoding.UTF8.GetString(bytes);

            return true;
        }

        public static string GetString(string key, string defaultValue = null)
        {
            string value = null;

            return TryGetString(key, out value) ? value : defaultValue;
        }

        //====== Object ======

        /// <summary> JSON に変換して保存する </summary>
        public static bool Set<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);

            return SetString(key, json);
        }

        public static bool TryGet<T>(string key, out T value)
        {
            value = default(T);

            string json = null;

            if (!TryGetString(key, out json)){ return false; }

            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException e)
            {
                Debug.LogError($"SecureStorage deserialize failed. (key = {key})\n{e}");

                return false;
            }

            return true;
        }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            T value = default(T);

            return TryGet(key, out value) ? value : defaultValue;
        }

        //====== Internal ======

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)){ throw new ArgumentException("Key is null or empty."); }
        }

        private static IStorage GetStorage()
        {
            if (storage == null)
            {
                #if UNITY_IOS && !UNITY_EDITOR

                storage = new KeychainStorage();

                #elif UNITY_ANDROID && !UNITY_EDITOR

                storage = new KeystoreStorage();

                #else

                storage = new PrefsStorage();

                #endif
            }

            return storage;
        }
    }
}

#endif
