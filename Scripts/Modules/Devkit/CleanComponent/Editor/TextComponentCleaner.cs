
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using Unity.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;
using Modules.GameText.Editor;


namespace Modules.Devkit.CleanComponent
{
    public class TextComponentCleaner : IComponentCleaner
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoClean
            {
                get { return ProjectPrefs.GetBool("TextComponentCleanerPrefs-autoClean", true); }
                set { ProjectPrefs.SetBool("TextComponentCleanerPrefs-autoClean", value); }
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
            var textComponents = allObjects.OfComponent<Text>();

            foreach (var textComponent in textComponents)
            {
                if (!string.IsNullOrEmpty(textComponent.text))
                {
                    textComponent.text = string.Empty;
                    EditorUtility.SetDirty(textComponent);
                }
            }

            GameTextLoader.Reload();
        }

        private static void OnApplyPrefab(GameObject prefab)
        {
            if (prefab == null) { return; }

            if (!Prefs.autoClean) { return; }

            var textComponents = prefab.DescendantsAndSelf().OfComponent<Text>();

            foreach (var textComponent in textComponents)
            {
                textComponent.text = string.Empty;
                EditorUtility.SetDirty(textComponent);

                var setter = UnityUtility.GetComponent<GameTextSetter>(textComponent.gameObject);

                if (setter != null)
                {
                    setter.ImportText();
                }
            }
        }
    }
}
