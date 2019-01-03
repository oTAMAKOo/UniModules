
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using Extensions;
using Modules.Atlas;
using Modules.Devkit.Prefs;
using UniRx.Async;

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

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyImageComponent(GameObject rootObject)
        {
            var imageComponents = rootObject.DescendantsAndSelf().OfComponent<Image>();

            foreach (var imageComponent in imageComponents)
            {
                if (imageComponent.sprite == null) { continue; }

                imageComponent.sprite = null;

                EditorUtility.SetDirty(imageComponent);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            var modify = gameObjects
                .SelectMany(x => x.DescendantsAndSelf().OfComponent<Image>())
                .Where(x => x.sprite != null)
                .Any(x => x.sprite.hideFlags == HideFlags.None);

            if (modify)
            {
                return EditorUtility.DisplayDialog("ImageComponent Cleaner", "Sprite is set directly Do you want to run cleanup?", "Execute", "Cancel");
            }

            return false;
        }
    }
}
