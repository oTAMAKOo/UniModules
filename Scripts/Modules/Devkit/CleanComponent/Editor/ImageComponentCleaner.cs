
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using Unity.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Atlas;
using Modules.Devkit.EventHook;
using Modules.Devkit.Prefs;


namespace Modules.Devkit.CleanComponent
{
    public class ImageComponentCleaner : IComponentCleaner
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

        [DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            PrefabApplyHook.OnApplyPrefabAsObservable().Subscribe(x => OnApplyPrefab(x));
        }

        public void Clean(GameObject[] allObjects)
        {
            var imageComponents = allObjects.OfComponent<Image>();

            foreach (var imageComponent in imageComponents)
            {
                if (imageComponent.sprite != null)
                {
                    imageComponent.sprite = null;

                    var atlasTextureImage = UnityUtility.GetComponent<AtlasTextureImage>(imageComponent.gameObject);

                    if (atlasTextureImage != null)
                    {
                        atlasTextureImage.Apply();
                    }

                    EditorUtility.SetDirty(imageComponent);
                }
            }
        }

        private static void OnApplyPrefab(GameObject prefab)
        {
            if (prefab == null) { return; }

            if (!Prefs.autoClean) { return; }

            var imageComponents = prefab.DescendantsAndSelf().OfComponent<Image>();

            foreach (var imageComponent in imageComponents)
            {
                imageComponent.sprite = null;

                var atlasTextureImage = UnityUtility.GetComponent<AtlasTextureImage>(imageComponent.gameObject);

                if (atlasTextureImage != null)
                {
                    atlasTextureImage.Apply();
                }

                EditorUtility.SetDirty(imageComponent);

            }
        }
    }
}
