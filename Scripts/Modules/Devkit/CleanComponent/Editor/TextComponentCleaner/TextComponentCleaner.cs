
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;
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
                get { return ProjectPrefs.GetBool("TextComponentCleanerPrefs-autoClean", false); }
                set { ProjectPrefs.SetBool("TextComponentCleanerPrefs-autoClean", value); }
            }
        }

        protected sealed class ComponentInfo<T> where T : class
        {
            public T Component{ get; private set; }
            public GameTextSetter GameTextSetter { get; private set; }

            public ComponentInfo(T component, GameTextSetter gameTextSetter)
            {
                Component = component;
                GameTextSetter = gameTextSetter;
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        protected static ComponentInfo<T>[] GetComponentInfos<T>(GameObject[] gameObjects) where T : Component
        {
            var list = new List<ComponentInfo<T>>();

            var components = gameObjects.OfComponent<T>();

            foreach (var component in components)
            {
                var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(component);

                list.Add(new ComponentInfo<T>(component, gameTextSetter));
            }

            return list.ToArray();
        }

        protected static bool CheckExecute(ComponentInfo<Text>[] textComponents, ComponentInfo<TextMeshProUGUI>[] textMeshProComponents)
        {
            Func<GameTextSetter, string, bool> checkContents = (gameTextSetter, text) =>
            {
                return gameTextSetter != null && gameTextSetter.Content == text;
            };

            var modify = false;

            modify |= textComponents
                .Where(x => !string.IsNullOrEmpty(x.Component.text))
                .Any(x => checkContents(x.GameTextSetter, x.Component.text));

            modify |= textMeshProComponents
                .Where(x => !string.IsNullOrEmpty(x.Component.text))
                .Any(x => checkContents(x.GameTextSetter, x.Component.text));

            if (modify)
            {
                return EditorUtility.DisplayDialog("TextComponent Cleaner", "Text is set directly Do you want to run cleanup?", "Clean", "Keep");
            }

            return false;
        }

        protected static void ApplyDevelopmentText<T>(ComponentInfo<T>[] targets) where T : Component
        {
            foreach (var target in targets)
            {
                var gameTextSetter = target.GameTextSetter;

                if (gameTextSetter == null) { continue; }

                gameTextSetter.ApplyDevelopmentText();
            }
        }
        
        protected static bool CleanDevelopmentText(ComponentInfo<Text>[] textComponents, ComponentInfo<TextMeshProUGUI>[] textMeshProComponents)
        {
            var changed = false;

            foreach (var textComponent in textComponents)
            {
                var gameTextSetter = textComponent.GameTextSetter;

                if (gameTextSetter == null){ continue; }

                var cleaned = gameTextSetter.CleanDevelopmentText();

                if (cleaned)
                {
                    EditorUtility.SetDirty(textComponent.Component);
                    changed = true;
                }
            }

            foreach (var textMeshProComponent in textMeshProComponents)
            {
                var gameTextSetter = textMeshProComponent.GameTextSetter;

                if (gameTextSetter == null) { continue; }

                var cleaned = gameTextSetter.CleanDevelopmentText();

                if (cleaned)
                {
                    EditorUtility.SetDirty(textMeshProComponent.Component);
                    changed = true;
                }
            }

            return changed;
        }

        protected static bool ModifyTextComponent(ComponentInfo<Text>[] textComponents, ComponentInfo<TextMeshProUGUI>[] textMeshProComponents)
        {
            var changed = false;

            foreach (var textComponent in textComponents)
            {
                if (string.IsNullOrEmpty(textComponent.Component.text)) { continue; }

                textComponent.Component.text = string.Empty;
                
                if (textComponent.GameTextSetter != null)
                {
                    textComponent.GameTextSetter.ImportText();

                    EditorUtility.SetDirty(textComponent.Component);

                    changed = true;
                }
            }
            
            foreach (var textMeshProComponent in textMeshProComponents)
            {
                if (string.IsNullOrEmpty(textMeshProComponent.Component.text)) { continue; }

                textMeshProComponent.Component.text = string.Empty;
                
                if (textMeshProComponent.GameTextSetter != null)
                {
                    textMeshProComponent.GameTextSetter.ImportText();

                    EditorUtility.SetDirty(textMeshProComponent.Component);

                    changed = true;
                }
            }

            return changed;
        }
    }
}
