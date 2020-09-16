﻿﻿﻿
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using Extensions;

namespace Modules.Devkit.Generators
{
    public static class SortingLayersScriptGenerator
    {
        private const string ScriptTemplate =
@"
// Generated by SortingLayersScriptGenerator.cs

using System.Collections.Generic;

namespace Constants
{
    public enum SortingLayer
    {
#ENUMS#
    }
}
";
        public static void Generate(string scriptPath)
        {
            var internalEditorUtilityType = typeof(InternalEditorUtility);
            var sortingLayerNamesProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            var sortingLayerNames = (string[])sortingLayerNamesProperty.GetValue(null, new object[0]);

            var sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            var sortingLayerUniqueIDs = (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);

            var enums = new StringBuilder();

            for (var i = 0; i < sortingLayerNames.Length; ++i)
            {
                enums.Append("\t\t").AppendFormat("{0} = {1},", sortingLayerNames[i], sortingLayerUniqueIDs[i]);

                // 最終行は改行しない.
                if (i < sortingLayerNames.Length - 1)
                {
                    enums.AppendLine();
                }
            }


            var script = ScriptTemplate;
            script = Regex.Replace(script, "#ENUMS#", enums.ToString());

            script = script.FixLineEnd();
            
            ScriptGenerateUtility.GenerateScript(scriptPath, @"SortingLayer.cs", script);
        }
    }
}
