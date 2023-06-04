
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public sealed class ValidateTextureSize
    {
        //----- params -----

        public class ValidateResult
        {
            public ValidateData validateData = null;

            public List<Texture> violationTextures = null;

            public List<Texture> ignoreTextures = null;

            public ValidateResult()
            {
                violationTextures = new List<Texture>();
                ignoreTextures = new List<Texture>();
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public Dictionary<string, ValidateData> GetTargetFolderInfos()
        {
            var config = TextureSizeValidateConfig.Instance;

            var validateData = config.GetValidateData();

            var validateDataByFolderPath = new Dictionary<string, ValidateData>();

            foreach (var item in validateData)
            {
                var folderPath = AssetDatabase.GUIDToAssetPath(item.folderGuid);

                folderPath = PathUtility.ConvertPathSeparator(folderPath) + PathUtility.PathSeparator;

                validateDataByFolderPath.Add(folderPath, item);
            }

            return validateDataByFolderPath;
        }

        public ValidateResult[] Validate()
        {
            var batchMode = Application.isBatchMode;

            var textureInfos = UnityEditorUtility.FindAssetsByType<Texture>("t:Texture")
                .Select(x => Tuple.Create(PathUtility.ConvertPathSeparator(AssetDatabase.GetAssetPath(x)), x))
                .ToArray();

            var config = TextureSizeValidateConfig.Instance;

            var ignoreFolderNames = config.GetIgnoreFolderNames();

            var ignorePaths = config.GetIgnoreGuids()
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.IsValidFolder(x) ? x + PathUtility.PathSeparator : x)
                .ToHashSet();

            var validateDataByFolderInfos = GetTargetFolderInfos();

            var folders = validateDataByFolderInfos
                .OrderByDescending(x => x.Key.Count(c => c == PathUtility.PathSeparator))
                .ThenBy(x => x.Key, new NaturalComparer())
                .ToArray();

            var data = new Dictionary<string, ValidateResult>();

            var count = textureInfos.Length;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var textureInfo = textureInfos[i];

                    var assetPath = textureInfo.Item1;
                    var texture = textureInfo.Item2;

                    if (!batchMode)
                    {
                        EditorUtility.DisplayProgressBar("Validate", assetPath, (float)i / count);
                    }

                    foreach (var folder in folders)
                    {
                        if (!assetPath.StartsWith(folder.Key)){ continue; }
                        
                        var validateInfo = data.GetValueOrDefault(folder.Key);

                        if(validateInfo == null)
                        {
                            validateInfo = new ValidateResult()
                            {
                                validateData = folder.Value,
                            };

                            data.Add(folder.Key, validateInfo);
                        }

                        if (IsIgnoreTarget(ignorePaths, ignoreFolderNames, assetPath))
                        {
                            if (!validateInfo.ignoreTextures.Contains(textureInfo.Item2))
                            {
                                validateInfo.ignoreTextures.Add(textureInfo.Item2);
                                break;
                            }
                        }
                        
                        if (folder.Value.width < texture.width || folder.Value.heigth < texture.height)
                        {
                            if (!validateInfo.violationTextures.Contains(texture))
                            {
                                validateInfo.violationTextures.Add(texture);
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!batchMode)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return data.Values.ToArray();
        }

        private bool IsIgnoreTarget(HashSet<string> ignorePaths, HashSet<string> ignoreFolderNames, string assetPath)
        {
            if (ignorePaths.Any(x => assetPath.StartsWith(x))){ return true; }

            if (assetPath.Split(PathUtility.PathSeparator).Any(x => ignoreFolderNames.Any(y => y == x))){ return true; }

            return false;
        }
    }
}