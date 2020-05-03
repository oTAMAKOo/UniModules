
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using System.Linq;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;
using Modules.UI.Extension;
using TMPro;

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

        protected static void ModifyTextComponent(GameObject rootObject)
        {
            var gameObjects = rootObject.DescendantsAndSelf().ToArray();

            var textComponents = gameObjects.OfComponent<Text>();

            foreach (var textComponent in textComponents)
            {
                if (string.IsNullOrEmpty(textComponent.text)) { continue; }

                textComponent.text = string.Empty;

                var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(textComponent);

                if (gameTextSetter != null)
                {
                    gameTextSetter.ImportText();
                }

                EditorUtility.SetDirty(textComponent);
            }

            var textMeshProComponents = gameObjects.OfComponent<TextMeshProUGUI>();

            foreach (var textMeshProComponent in textMeshProComponents)
            {
                if (string.IsNullOrEmpty(textMeshProComponent.text)) { continue; }

                textMeshProComponent.text = string.Empty;

                var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(textMeshProComponent);

                if (gameTextSetter != null)
                {
                    gameTextSetter.ImportText();
                }

                EditorUtility.SetDirty(textMeshProComponent);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            Func<GameObject, string, bool> checkContents = (go, text) =>
            {
                var execute = true;

                var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(go);

                if (gameTextSetter != null)
                {
                    if (gameTextSetter.Content == text)
                    {
                        execute = false;
                    }

                    if (gameTextSetter.GetDevelopmentText() == text)
                    {
                        execute = false;
                    }
                }

                return execute;
            };

            var modify = false;

            modify |= gameObjects.SelectMany(x => x.DescendantsAndSelf().OfComponent<Text>())
                .Where(x => !string.IsNullOrEmpty(x.text))
                .Any(x => checkContents(x.gameObject, x.text));

            modify |= gameObjects.SelectMany(x => x.DescendantsAndSelf().OfComponent<TextMeshProUGUI>())
                .Where(x => !string.IsNullOrEmpty(x.text))
                .Any(x => checkContents(x.gameObject, x.text));

            if (modify)
            {
                return EditorUtility.DisplayDialog("TextComponent Cleaner", "Text is set directly Do you want to run cleanup?", "Clean", "Keep");
            }

            return false;
        }
    }
}
