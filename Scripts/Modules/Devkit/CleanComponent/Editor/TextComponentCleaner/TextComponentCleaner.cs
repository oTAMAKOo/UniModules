
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
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

        protected static bool CheckExecute<T>(ComponentInfo<T>[] targets, Func<T, string> getTextCallback) where T : Component
        {
            Func<GameTextSetter, string, bool> checkContents = (gameTextSetter, text) =>
            {
                if (string.IsNullOrEmpty(text)) { return false; }

                return gameTextSetter != null && gameTextSetter.Content == text;
            };
            
            var modify = targets.Any(x => checkContents(x.GameTextSetter, getTextCallback.Invoke(x.Component)));

            return modify;
        }

        protected static bool ConfirmExecute()
        {
            var title = "TextComponent Cleaner";
            var message = "Text is set directly Do you want to run cleanup?";

            var execute = EditorUtility.DisplayDialog(title, message, "Clean", "Keep");

            return execute;
        }

        protected static void ApplyDevelopmentText<T>(ComponentInfo<T>[] targets) where T : Component
        {
            foreach (var target in targets)
            {
                var gameTextSetter = target.GameTextSetter;

                if (gameTextSetter == null) { continue; }
                
                Reflection.InvokePrivateMethod(gameTextSetter, "ApplyDevelopmentText");
            }
        }
        
        protected static bool CleanDevelopmentText<T>(ComponentInfo<T>[] targets) where T : Component
        {
            var changed = false;

            foreach (var target in targets)
            {
                var gameTextSetter = target.GameTextSetter;

                if (gameTextSetter == null){ continue; }

                var cleaned = (bool)Reflection.InvokePrivateMethod(gameTextSetter, "CleanDevelopmentText");

                if (cleaned)
                {
                    EditorUtility.SetDirty(target.Component);
                    changed = true;
                }
            }

            return changed;
        }

        protected static bool ModifyTextComponent<T>(ComponentInfo<T>[] targets, Func<T, bool> checkEmptyCallback, Action<T> cleanCallback) where T : Component
        {
            var changed = false;

            foreach (var target in targets)
            {
                if (checkEmptyCallback.Invoke(target.Component)) { continue; }

                cleanCallback.Invoke(target.Component);

                if (target.GameTextSetter != null)
                {
                    Reflection.InvokePrivateMethod(target.GameTextSetter, "ImportText");

                    EditorUtility.SetDirty(target.Component);

                    changed = true;
                }
            }
            
            return changed;
        }
    }
}
