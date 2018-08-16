﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Atlas
{
	public class AtlasTextureSaveHook : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.IsEmpty()) { return paths; }

            GameObject[] hierarchyGameObjects = null;

            foreach (string path in paths)
            {
                var atlasTexture = AssetDatabase.LoadMainAssetAtPath(path) as AtlasTexture;

                if (atlasTexture != null)
                {
                    // 必要になった時に一回だけ取得して使いまわす.
                    if (hierarchyGameObjects == null)
                    {
                        hierarchyGameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();
                    }

                    // AtlasTextureImage.
                    UpdateAtlasTextureImage(hierarchyGameObjects, atlasTexture);
                }
            }

            return paths;
        }

        private static void UpdateAtlasTextureImage(GameObject[] gameObjects, AtlasTexture atlas)
        {
            var atlasAssetPath = AssetDatabase.GetAssetPath(atlas);

            foreach (var gameObject in gameObjects)
            {
                var atlasTextureImage = UnityUtility.GetComponent<AtlasTextureImage>(gameObject);
                
                if (atlasTextureImage != null && atlasTextureImage.Atlas != null)
                {
                    var path = AssetDatabase.GetAssetPath(atlasTextureImage.Atlas);

                    if (path == atlasAssetPath)
                    {
                        atlasTextureImage.Apply();
                    }
                }
            }
        }
    }
}