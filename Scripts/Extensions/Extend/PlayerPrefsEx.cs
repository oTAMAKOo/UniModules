
using UnityEngine;
using System;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Extensions
{
    public static class PlayerPrefsEx
    {
        private const string PrefsKey = "Sa0HbfDqeF6hw4s1";

        private static readonly AesManaged aesManaged = AESExtension.CreateAesManaged(PrefsKey);

        //====== Utility ======

        public static bool HasKey(string name)
        {
            return PlayerPrefs.HasKey(name.Encrypt(aesManaged));
        }

        public static void DeleteKey(string name)
        {
            PlayerPrefs.DeleteKey(name.Encrypt(aesManaged));
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

        public static void SetString(string name, string value)
        {
            PlayerPrefs.SetString(name.Encrypt(aesManaged), value.Encrypt(aesManaged));
        }

        public static string GetString(string name, string defaultValue = "")
        {
            if (!HasKey(name)) { return defaultValue; }

            return PlayerPrefs.GetString(name.Encrypt(aesManaged)).Decrypt(aesManaged);
        }

        //====== Int ======

        public static void SetInt(string name, int value)
        {
            SetString(name, value.ToString());
        }

        public static int GetInt(string name, int defaultValue = 0)
        {
            if (!HasKey(name)) { return defaultValue; }

            var value = GetString(name);

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

        public static void SetFloat(string name, float value)
        {
            SetString(name, value.ToString());
        }

        public static float GetFloat(string name, float defaultValue = 0f)
        {
            if (!HasKey(name)) { return defaultValue; }

            var value = GetString(name);

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

        public static void SetBool(string name, bool value)
        {
            SetInt(name, value ? 1 : 0);
        }

        public static bool GetBool(string name, bool defaultValue = false)
        {
            return GetInt(name, defaultValue ? 1 : 0) != 0;
        }

        //====== DateTime ======

        public static void SetDateTime(string name, DateTime value)
        {
            SetString(name, value.ToString());
        }

        public static DateTime GetDateTime(string name, DateTime? defaultValue = null)
        {
            if (!defaultValue.HasValue)
            {
                defaultValue = DateTime.MinValue;
            }

            var value = GetString(name);

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

        public static void SetColor(string name, Color value)
        {
            SetString(name, string.Format("{0},{1},{2},{3}", value.r, value.g, value.b, value.a));
        }

        public static Color GetColor(string name, Color? defaultValue = null)
        {
            if (!defaultValue.HasValue)
            {
                defaultValue = Color.white;
            }

            var value = GetString(name);

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

        public static void SetEnum(string name, System.Enum value)
        {
            SetString(name, value.ToString());
        }

        public static T GetEnum<T>(string name, T defaultValue = default(T))
        {
            var val = GetString(name, defaultValue.ToString());

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

        public static void Set<T>(string name, T value)
        {
            var json = JsonConvert.SerializeObject(value);

            SetString(name, json);
        }

        public static T Get<T>(string name, T defaultValue = default(T))
        {
            var json = GetString(name, null);

            return string.IsNullOrEmpty(json) ? defaultValue : JsonConvert.DeserializeObject<T>(json);
        }
    }
}
