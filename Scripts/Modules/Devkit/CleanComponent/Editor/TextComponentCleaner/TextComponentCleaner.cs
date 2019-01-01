
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;

namespace Modules.Devkit.CleanComponent
{
    public abstract class TextComponentCleaner
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

        protected static bool ModifyTextComponent(GameObject rootObject)
        {
            var modified = false;
            var textComponents = rootObject.DescendantsAndSelf().OfComponent<Text>();

            foreach (var textComponent in textComponents)
            {
                if (string.IsNullOrEmpty(textComponent.text)) { continue; }

                var before = textComponent.text;

                textComponent.text = string.Empty;

                var setter = UnityUtility.GetComponent<GameTextSetter>(textComponent.gameObject);

                if (setter != null)
                {
                    setter.ImportText();
                }

                var after = textComponent.text;

                if (before != after)
                {
                    modified = true;
                    EditorUtility.SetDirty(textComponent);
                }
            }

            return modified;
        }
    }
}
