
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.CleanComponent
{
    public abstract class ImageComponentCleaner
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoClean
            {
                get { return ProjectPrefs.GetBool("ImageComponentCleanerPrefs-autoClean", true); }
                set { ProjectPrefs.SetBool("ImageComponentCleanerPrefs-autoClean", value); }
            }
        }

        private const string UnityBuiltinAssetPath = "Resources/unity_builtin_extra";

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyImageComponent(GameObject rootObject)
        {
            var imageComponents = rootObject.DescendantsAndSelf().OfComponent<Image>();

            foreach (var imageComponent in imageComponents)
            {
                if (!CheckModifyTarget(imageComponent)) { continue; }

                imageComponent.sprite = null;

                EditorUtility.SetDirty(imageComponent);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            var modify = gameObjects
                .SelectMany(x => x.DescendantsAndSelf().OfComponent<Image>())
                .Any(x => CheckModifyTarget(x));

            if (modify)
            {
                return EditorUtility.DisplayDialog("ImageComponent Cleaner", "Sprite is set directly Do you want to run cleanup?", "Execute", "Cancel");
            }

            return false;
        }

        private static bool CheckModifyTarget(Image image)
        {
            // Spriteが存在.
            if(image.sprite == null) { return false; }

            // Unity内臓のAssetの場合は許容.
            if(AssetDatabase.GetAssetPath(image.mainTexture) == UnityBuiltinAssetPath) { return false; }

            // 保存されないなら許容.
            if(image.sprite.hideFlags.HasFlag(HideFlags.DontSaveInEditor)) { return false; }

            return true;
        }
    }
}
