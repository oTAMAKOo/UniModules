
using System;
using System.Security.Cryptography;
using UnityEngine;
using JsonFx.Json;

namespace Extensions
{
    public static class PlayerPrefsEx
    {
        private const string PrefsKey = "Sa0HbfDqeF6hw4s1";

        private static readonly RijndaelManaged rijndael = AESExtension.CreateRijndael(PrefsKey);

        //====== Utility ======

        public static bool HasKey(string name)
        {
            return PlayerPrefs.HasKey(name.Encrypt(rijndael));
        }

        public static void DeleteKey(string name)
        {
            PlayerPrefs.DeleteKey(name.Encrypt(rijndael));
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
            PlayerPrefs.SetString(name.Encrypt(rijndael), value.Encrypt(rijndael));
        }

        public static string GetString(string name, string defaultValue = "")
        {
            if (!HasKey(name)) { return defaultValue; }

            return PlayerPrefs.GetString(name.Encrypt(rijndael)).Decrypt(rijndael);
        }

        //====== Int ======

        public static void SetInt(string name, int value)
        {
            PlayerPrefs.SetInt(name.Encrypt(rijndael), value);
        }

        public static int GetInt(string name, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(name.Encrypt(rijndael), defaultValue);
        }

        //====== Float ======

        public static void SetFloat(string name, float value)
        {
            PlayerPrefs.SetFloat(name.Encrypt(rijndael), value);
        }

        public static float GetFloat(string name, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(name.Encrypt(rijndael), defaultValue);
        }

        //====== Bool ======

        public static void SetBool(string name, bool value)
        {
            PlayerPrefs.SetInt(name.Encrypt(rijndael), value ? 1 : 0);
        }

        public static bool GetBool(string name, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(name.Encrypt(rijndael), defaultValue ? 1 : 0) != 0;
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

            var str = GetString(name);

            if (string.IsNullOrEmpty(str)) { return defaultValue.Value; }

            return DateTime.Parse(str);
        }

        //====== Color ======

        public static void SetColor(string name, Color value)
        {
            SetString(name, value.r + " " + value.g + " " + value.b + " " + value.a);
        }

        public static Color GetColor(string name, Color? value = null)
        {
            if (!value.HasValue)
            {
                value = Color.white;
            }

            var c = value.Value;

            var strVal = GetString(name, c.r + " " + c.g + " " + c.b + " " + c.a);
            var parts = strVal.Split(' ');

            if (parts.Length == 4)
            {
                float.TryParse(parts[0], out c.r);
                float.TryParse(parts[1], out c.g);
                float.TryParse(parts[2], out c.b);
                float.TryParse(parts[3], out c.a);
            }

            return c;
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
            var json = JsonWriter.Serialize(value);

            SetString(name, json);
        }

        public static T Get<T>(string name, T defaultValue = default(T))
        {
            var json = GetString(name, null);

            return string.IsNullOrEmpty(json) ? defaultValue : JsonReader.Deserialize<T>(json);
        }
    }
}