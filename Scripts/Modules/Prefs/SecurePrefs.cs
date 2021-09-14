
using UnityEngine;
using System;
using Newtonsoft.Json;

namespace Extensions
{
    public static class SecurePrefs
    {
        private static string keyPrefix = string.Empty;

        private static AesCryptoKey aesCryptoKey = null;

        //====== CryptKey ======

        public static void SetCryptoKey(AesCryptoKey aesCryptoKey)
        {
            SecurePrefs.aesCryptoKey = aesCryptoKey;
        }

        private static AesCryptoKey GetCryptoKey()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey("5kaDpFGc1A9iRaLkv2n3dMxCmjjFzxOX", "1i233x1fs8J1K9Tp");
            }

            return aesCryptoKey;
        }

        //====== Key ======

        private static void SetKeyPrefix(string prefix)
        {
            keyPrefix = prefix;
        }

        private static string GetKey(string keyName)
        {
            var cryptoKey = GetCryptoKey();

            var key = string.IsNullOrEmpty(keyPrefix) ? keyName : $"{ keyPrefix }-{ keyName }"; 

            return key.Encrypt(cryptoKey);
        }

        //====== Utility ======

        public static bool HasKey(string keyName)
        {
            var key = GetKey(keyName);

            return PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string keyName)
        {
            var key = GetKey(keyName);

            PlayerPrefs.DeleteKey(key);
        }

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        //====== String ======

        public static void SetString(string keyName, string value)
        {
            var key = GetKey(keyName);

            var cryptoKey = GetCryptoKey();

            PlayerPrefs.SetString(key, value.Encrypt(cryptoKey, true));
        }

        public static string GetString(string keyName, string defaultValue = "")
        {
            if (!HasKey(keyName)) { return defaultValue; }

            var cryptoKey = GetCryptoKey();

            var key = GetKey(keyName);

            return PlayerPrefs.GetString(key).Decrypt(cryptoKey, true);
        }

        //====== Int ======

        public static void SetInt(string keyName, int value)
        {
            SetString(keyName, value.ToString());
        }

        public static int GetInt(string keyName, int defaultValue = 0)
        {
            if (!HasKey(keyName)) { return defaultValue; }

            var value = GetString(keyName);

            if (string.IsNullOrEmpty(value)) { return defaultValue; }

            try
            {
                return int.Parse(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        //====== Float ======

        public static void SetFloat(string keyName, float value)
        {
            SetString(keyName, value.ToString());
        }

        public static float GetFloat(string keyName, float defaultValue = 0f)
        {
            if (!HasKey(keyName)) { return defaultValue; }

            var value = GetString(keyName);

            if (string.IsNullOrEmpty(value)) { return defaultValue; }

            try
            {
                return float.Parse(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        //====== Bool ======

        public static void SetBool(string keyName, bool value)
        {
            SetInt(keyName, value ? 1 : 0);
        }

        public static bool GetBool(string keyName, bool defaultValue = false)
        {
            return GetInt(keyName, defaultValue ? 1 : 0) != 0;
        }

        //====== DateTime ======

        public static void SetDateTime(string keyName, DateTime value)
        {
            SetString(keyName, value.ToString());
        }

        public static DateTime GetDateTime(string keyName, DateTime? defaultValue = null)
        {
            if (!defaultValue.HasValue)
            {
                defaultValue = DateTime.MinValue;
            }

            var value = GetString(keyName);

            if (string.IsNullOrEmpty(value)) { return defaultValue.Value; }

            try
            {
                return DateTime.Parse(value);
            }
            catch
            {
                return defaultValue.Value;
            }
        }

        //====== Color ======

        public static void SetColor(string keyName, Color value)
        {
            SetString(keyName, string.Format("{0},{1},{2},{3}", value.r, value.g, value.b, value.a));
        }

        public static Color GetColor(string keyName, Color? defaultValue = null)
        {
            if (!defaultValue.HasValue)
            {
                defaultValue = Color.white;
            }

            var value = GetString(keyName);

            if (string.IsNullOrEmpty(value)) { return defaultValue.Value; }

            var parts = value.Split(',');

            if (parts.Length != 4) { return defaultValue.Value; }

            try
            {
                var r = float.Parse(parts[0]);
                var g = float.Parse(parts[1]);
                var b = float.Parse(parts[2]);
                var a = float.Parse(parts[3]);

                return new Color(r, g, b, a);
            }
            catch
            {
                return defaultValue.Value;
            }
        }

        //====== Enum ======

        public static void SetEnum(string keyName, System.Enum value)
        {
            SetString(keyName, value.ToString());
        }

        public static T GetEnum<T>(string keyName, T defaultValue = default(T))
        {
            var val = GetString(keyName, defaultValue.ToString());

            var names = System.Enum.GetNames(typeof(T));

            var values = System.Enum.GetValues(typeof(T));

            for (var i = 0; i < names.Length; ++i)
            {
                if (names[i] == val)
                {
                    return (T)values.GetValue(i);
                }
            }

            return defaultValue;
        }

        //====== Generic ======

        public static void Set<T>(string keyName, T value)
        {
            var json = JsonConvert.SerializeObject(value);

            SetString(keyName, json);
        }

        public static T Get<T>(string keyName, T defaultValue = default(T))
        {
            var json = GetString(keyName, null);

            return string.IsNullOrEmpty(json) ? defaultValue : JsonConvert.DeserializeObject<T>(json);
        }
    }
}
