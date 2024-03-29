﻿
#if ENABLE_CRIWARE_SOFDEC

using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Extensions;
using Modules.CriWare;
using Modules.Devkit.Generators;
using Modules.Devkit.Project;

namespace Modules.Movie.Editor
{
    public static class MovieScriptGenerator
    {
        //----- params -----

        private const string ScriptTemplate =
@"
// Generated by MovieScriptGenerator.cs

#if ENABLE_CRIWARE_SOFDEC

using System.Collections.Generic;
using CriWare;
using Extensions;
using Modules.Movie;

namespace @NAMESPACE
{
    public static partial class Movies
	{
        public enum Mana
        {
@ENUMS
        }

        private static Dictionary<Mana, ManaInfo> internalMovies = new Dictionary<Mana, ManaInfo>()
        {
@CONTENTS
        };

        public static ManaInfo GetManaInfo(Mana mana)
        {
            var fileDirectory = string.Empty;

            #if UNITY_EDITOR

            var editorStreamingAssetsFolderPath = @EDITOR_STREAMING_ASSETS_FOLDER_PATH;

            fileDirectory = UnityPathUtility.ConvertAssetPathToFullPath(editorStreamingAssetsFolderPath);

            #else

            fileDirectory = Common.streamingAssetsPath;

            #endif

            var info = internalMovies.GetValueOrDefault(mana);

            var path = PathUtility.Combine(fileDirectory, info.UsmPath);

            return new ManaInfo(path);
        }
    }
}

#endif
";

        private const string EnumTemplate = @"{0},";
        private const string ContentsTemplate = @"{{ Mana.{0}, new ManaInfo(""{1}"") }},";

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(string scriptPath, string scriptNamespace, string rootFolderPath, string rootFolderName)
        {
            var projectUnityFolders = ProjectUnityFolders.Instance;

            var infos = LoadUsmInfo(rootFolderPath);

            var enums = new StringBuilder();
            var contents = new StringBuilder();

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];

                var assetPath = info.Usm.Replace(rootFolderPath + PathUtility.PathSeparator, string.Empty);
                var enumName = ScriptGenerateUtility.GetCSharpName(PathUtility.GetPathWithoutExtension(assetPath), false);

				var usmPath = PathUtility.Combine(rootFolderName, info.UsmPath);

                enums.Append("\t\t\t").AppendFormat(EnumTemplate, enumName);
                contents.Append("\t\t\t").AppendFormat(ContentsTemplate, enumName, usmPath);

                if (i < infos.Length - 1)
                {
                    enums.AppendLine();
                    contents.AppendLine();
                }
            }

            var editorStreamingAssetsFolderPath = projectUnityFolders.StreamingAssetPath;

            var script = ScriptTemplate;

            script = Regex.Replace(script, "@NAMESPACE", scriptNamespace);
            script = Regex.Replace(script, "@ENUMS", enums.ToString());
            script = Regex.Replace(script, "@CONTENTS", contents.ToString());
            script = Regex.Replace(script, "@EDITOR_STREAMING_ASSETS_FOLDER_PATH", @"""" + editorStreamingAssetsFolderPath + @"""");

            ScriptGenerateUtility.GenerateScript(scriptPath, @"Movies.cs", script);
        }

        private static ManaInfo[] LoadUsmInfo(string path)
        {
            var result = new List<ManaInfo>();

            var guids = AssetDatabase.FindAssets(string.Empty, new string[] { path });

            var usmAssets = guids
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => Path.GetExtension(x) == CriAssetDefinition.UsmExtension)
                .Select(x => AssetDatabase.LoadMainAssetAtPath(x))
                .ToArray();

            foreach (var usmAsset in usmAssets)
            {
                var assetPath = AssetDatabase.GetAssetPath(usmAsset);

				assetPath = assetPath.Replace(path + PathUtility.PathSeparator, string.Empty);

                result.Add(new ManaInfo(assetPath));
            }

            return result.ToArray();
        }
    }
}

#endif
