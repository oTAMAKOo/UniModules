
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using Extensions;
using Modules.Atlas;
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

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyImageComponent(GameObject rootObject)
        {
            var imageComponents = rootObject.DescendantsAndSelf().OfComponent<Image>();

            foreach (var imageComponent in imageComponents)
            {
                var before = imageComponent.sprite;

                if (imageComponent.sprite == null) { continue; }

                imageComponent.sprite = null;

                var atlasTextureImage = UnityUtility.GetComponent<AtlasTextureImage>(imageComponent);

                if(atlasTextureImage != null)
                {
                    atlasTextureImage.Apply();
                }

                var after = imageComponent.sprite;

                if (before != after)
                {
                    EditorUtility.SetDirty(imageComponent);
                }
            }
        }
    }
}
