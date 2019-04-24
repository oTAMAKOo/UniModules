﻿﻿
using UnityEngine;
using Newtonsoft.Json;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Modules.Devkit.Prefs
{

#if UNITY_EDITOR

    public static class ProjectPrefs
	{
        //----- params -----

        //----- field -----

        private static string identifier = null;

        //----- property -----

        private static string ProjectIdentifier
        {
            get
            {
                return identifier ?? (identifier = string.Format("[{0}]:", Application.dataPath.GetHashCode().ToString()));
            }
        }

        //----- method -----

        public static bool HasKey(string key)
        {
            return EditorPrefs.HasKey(ProjectIdentifier + key);
        }

        public static void DeleteKey(string key)
        {
            EditorPrefs.DeleteKey(ProjectIdentifier + key);
        }

        //====== bool ======
        
        public static bool GetBool(string key, bool defaultValue = false)
        {
            return EditorPrefs.GetBool(ProjectIdentifier + key, defaultValue);
        }

        public static void SetBool(string key, bool value)
        {
            EditorPrefs.SetBool(ProjectIdentifier + key, value);
        }

        //====== string ======

        public static string GetString(string key, string defaultValue = "")
        {
            return EditorPrefs.GetString(ProjectIdentifier + key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            EditorPrefs.SetString(ProjectIdentifier + key, value);
        }

        //====== int ======

        public static int GetInt(string key, int defaultValue = 0)
        {
            return EditorPrefs.GetInt(ProjectIdentifier + key, defaultValue);
        }

        public static void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(ProjectIdentifier + key, value);
        }

        //====== float ======

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return EditorPrefs.GetFloat(ProjectIdentifier + key, defaultValue);
        }

        public static void SetFloat(string key, float value)
        {
            EditorPrefs.SetFloat(ProjectIdentifier + key, value);
        }

        //====== Enum ======

        public static T GetEnum<T>(string key, T defaultValue)
        {
            var val = GetString(key, defaultValue.ToString());

            var names = System.Enum.GetNames(typeof(T));
            var values = System.Enum.GetValues(typeof(T));

            for (int i = 0; i < names.Length; ++i)
            {
                if (names[i] == val)
                {
                    return (T)values.GetValue(i);
                }
            }

            return defaultValue;
        }

        public static void SetEnum(string key, System.Enum value)
        {
            SetString(key, value.ToString());
        }

        //====== Color ======

        public static Color GetColor(string key, Color defaultValue = new Color())
        {
            var color = new Color();

            var defaultStr = defaultValue.r + " " + defaultValue.g + " " + defaultValue.b + " " + defaultValue.a;

            var value = GetString(key, defaultStr);

            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(' ');

                if (parts.Length == 4)
                {
                    float.TryParse(parts[0], out color.r);
                    float.TryParse(parts[1], out color.g);
                    float.TryParse(parts[2], out color.b);
                    float.TryParse(parts[3], out color.a);
                }
            }
            else
            {
                color = defaultValue;
            }

            return color;
        }

        public static void SetColor(string key, Color value)
        {
            SetString(key, value.r + " " + value.g + " " + value.b + " " + value.a);
        }

        //====== Class ======

        public static T Get<T>(string key, T defaultValue)
        {
            var json = GetString(key);

            return string.IsNullOrEmpty(json) ? defaultValue : JsonConvert.DeserializeObject<T>(json);
        }

        public static void Set<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);

            SetString(key, json);
        }

        //====== Asset ======

        public static T GetAsset<T>(string key, T defaultValue) where T : UnityEngine.Object
        {
            var path = GetString(key);

            if (string.IsNullOrEmpty(path)) { return null; }

            var retVal = LoadAsset<T>(path);

            if (retVal == null)
            {
                int id;

                if (int.TryParse(path, out id))
                {
                    return EditorUtility.InstanceIDToObject(id) as T;
                }
            }

            return retVal;
        }

        public static void SetAsset(string key, UnityEngine.Object obj)
        {
            if (obj == null)
            {
                DeleteKey(key);
            }
            else
            {
                if (obj != null)
                {
                    string path = AssetDatabase.GetAssetPath(obj);

                    if (!string.IsNullOrEmpty(path))
                    {
                        SetString(key, path);
                    }
                    else
                    {
                        SetString(key, obj.GetInstanceID().ToString());
                    }
                }
                else
                {
                    DeleteKey(key);
                }
            }
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path)) return null;
            var obj = AssetDatabase.LoadMainAssetAtPath(path);

            if (obj == null) return null;

            T val = obj as T;
            if (val != null) return val;

            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                if (obj.GetType() == typeof(GameObject))
                {
                    GameObject go = obj as GameObject;
                    return go.GetComponent(typeof(T)) as T;
                }
            }
            return null;
        }
    }
#endif
}
