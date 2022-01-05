
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Extensions
{
    public static class TextureEditorUtility
    {
        //----- params -----

        //----- field -----

        private static Type textureUtilType = null;

        private static Dictionary<string, MethodInfo> methodInfoCache = null;

        //----- property -----

        //----- method -----

        static TextureEditorUtility()
        {
            methodInfoCache = new Dictionary<string, MethodInfo>();
        }

        private static Type GetTextureUtilType()
        {
            if (textureUtilType != null) { return textureUtilType; }

            var assembly = typeof(EditorApplication).Assembly;

            textureUtilType = assembly.GetTypes().FirstOrDefault(t => t.FullName == "UnityEditor.TextureUtil");

            if (textureUtilType == null)
            {
                throw new Exception("TextureUtil not found.");
            }

            return textureUtilType;
        }

        private static MethodInfo GetMethodInfo(string methodName)
        {
            var methodInfo = methodInfoCache.GetValueOrDefault(methodName);

            if (methodInfo == null)
            {
                var textureUtilType = GetTextureUtilType();

                methodInfo = textureUtilType.GetMethod(methodName);

                if (methodInfo != null)
                {
                    methodInfoCache[methodName] = methodInfo;
                }
                else
                {
                    throw new Exception(methodName + " not found.");
                }
            }

            return methodInfo;
        }

        public static long GetStorageMemorySizeLong(Texture texture)
        {
            var methodInfo = GetMethodInfo("GetStorageMemorySizeLong");

            if (methodInfo == null){ return 0; }

            var argument = new Texture[] { texture };

            return (long)methodInfo.Invoke(null, argument);
        }

        public static long GetRuntimeMemorySizeLong(Texture texture)
        {
            var methodInfo = GetMethodInfo("GetRuntimeMemorySizeLong");

            if (methodInfo == null){ return 0; }

            var argument = new Texture[] { texture };

            return (long)methodInfo.Invoke(null, argument);
        }
    }
}