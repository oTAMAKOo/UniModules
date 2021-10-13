
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Devkit.Inspector
{
    public abstract class RegisterScrollView<T> : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        protected List<T> contents = null;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private Subject<T[]> onUpdateContents = null;

        //----- property -----

        public IReadOnlyList<T> Contents
        {
            get { return contents; }
        }

        //----- method -----

        public RegisterScrollView()
        {
            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
        }

        public void SetContents(T[] contents)
        {
            this.contents = contents.ToList();
        }

        public virtual void DrawGUI(params GUILayoutOption[] options)
        {
            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawHeaderContent();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(toolbarPlusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                {
                    contents.Add(CreateNewContent());
                }
            }

            GUILayout.Space(2f);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, options))
            {
                for (var index = 0; index < contents.Count; index++)
                {
                    var content = Contents.ElementAtOrDefault(index);

                    if (content == null){ continue; }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        content = DrawContent(index, content);

                        if (EditorGUI.EndChangeCheck())
                        {
                            contents[index] = content;

                            ValidateContent(content);

                            if (onUpdateContents != null)
                            {
                                onUpdateContents.OnNext(Contents.ToArray());
                            }
                        }

                        if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                        {
                            contents.RemoveAt(index);
                            
                            if (onUpdateContents != null)
                            {
                                onUpdateContents.OnNext(contents.ToArray());
                            }
                        }
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }

            GUILayout.Space(2f);
        }

        public IObservable<T[]> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<T[]>());
        }

        protected virtual void DrawHeaderContent() { }

        protected virtual void ValidateContent(T content) { }

        protected abstract T CreateNewContent();

        protected abstract T DrawContent(int index, T content);
    }
}
