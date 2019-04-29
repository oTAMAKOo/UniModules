
using UnityEngine;
using UnityEditor;
using Unity.Linq;

namespace Modules.SpriteSheet
{
    public static class SpriteSheetUpdateHook
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabUtility.prefabInstanceUpdated += ApplySpriteSheetImage;
        }

        private static void ApplySpriteSheetImage(GameObject instance)
        {
            var spriteSheetImages = instance.DescendantsAndSelf().OfComponent<SpriteSheetImage>();

            foreach (var spriteSheetImage in spriteSheetImages)
            {
                if (spriteSheetImage.SpriteSheet != null)
                {
                    spriteSheetImage.Apply();
                }
            }
        }
    }
}
