﻿﻿
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.SpriteSheet
{
	public class SpriteSheetSaveHook : UnityEditor.AssetModificationProcessor
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
                var spriteSheet = AssetDatabase.LoadMainAssetAtPath(path) as SpriteSheet;

                if (spriteSheet != null)
                {
                    // 必要になった時に一回だけ取得して使いまわす.
                    if (hierarchyGameObjects == null)
                    {
                        hierarchyGameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();
                    }
                    
                    UpdateSpriteSheetImage(hierarchyGameObjects, spriteSheet);
                }
            }

            return paths;
        }

        private static void UpdateSpriteSheetImage(GameObject[] gameObjects, SpriteSheet spriteSheet)
        {
            var spriteSheetPath = AssetDatabase.GetAssetPath(spriteSheet);

            foreach (var gameObject in gameObjects)
            {
                var spriteSheetImage = UnityUtility.GetComponent<SpriteSheetImage>(gameObject);
                
                if (spriteSheetImage != null && spriteSheetImage.SpriteSheet != null)
                {
                    var path = AssetDatabase.GetAssetPath(spriteSheetImage.SpriteSheet);

                    if (path == spriteSheetPath)
                    {
                        spriteSheetImage.Apply();
                    }
                }
            }
        }
    }
}
