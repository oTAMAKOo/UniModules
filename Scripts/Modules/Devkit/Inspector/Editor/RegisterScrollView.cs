
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using UniRx;
using UnityEditor;

namespace Modules.Devkit.Inspector
{
    public abstract class RegisterScrollView<T> : LifetimeDisposable
    {
        //----- params -----

        private sealed class FastScrollView : EditorGUIFastScrollView<T>
        {
            private RegisterScrollView<T> instance = null;
            
            public override Direction Type { get { return Direction.Vertical; } }

            public FastScrollView(RegisterScrollView<T> instance)
            {
                this.instance = instance;                
            }

            protected override void DrawContent(int index, T content)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    content = instance.DrawContent(index, content);

                    if (EditorGUI.EndChangeCheck())
                    {
                        var list = Contents.ToList();

                        list[index] = content;

                        Contents = list.ToArray();

                        if (instance.onUpdateContents != null)
                        {
                            instance.onUpdateContents.OnNext(Contents);
                        }
                    }

                    if (GUILayout.Button(instance.toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                    {
                        var list = Contents.ToList();

                        list.RemoveAt(index);

                        Contents = list.ToArray();

                        if (instance.onUpdateContents != null)
                        {
                            instance.onUpdateContents.OnNext(Contents);
                        }

                        RequestRepaint();
                    }
                }
            }
        }

        //----- field -----

        private FastScrollView scrollView = null;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private Subject<T[]> onUpdateContents = null;
        private Subject<Unit> onRepaintRequest = null;

        //----- property -----

        public T[] Contents
        {
            get { return scrollView.Contents; }
            protected set { scrollView.Contents = value; }
        }

        //----- method -----

        public RegisterScrollView()
        {
            scrollView = new FastScrollView(this);

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");

            scrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ =>
                    {
                        if (onRepaintRequest != null)
                        {
                            onRepaintRequest.OnNext(Unit.Default);
                        }
                    })
                .AddTo(Disposable);
        }

        public void SetContents(T[] contents)
        {
            scrollView.Contents = contents;
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
                    var list = Contents.ToList();

                    list.Add(CreateNewContent());

                    Contents = list.ToArray();

                    if (onRepaintRequest != null)
                    {
                        onRepaintRequest.OnNext(Unit.Default);
                    }
                }
            }

            GUILayout.Space(2f);

            scrollView.Draw(true, options);
        }

        public IObservable<T[]> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<T[]>());
        }

        public IObservable<Unit> OnRepaintRequestAsObservable()
        {
            return onRepaintRequest ?? (onRepaintRequest = new Subject<Unit>());
        }

        protected virtual void DrawHeaderContent() { }

        protected abstract T CreateNewContent();

        protected abstract T DrawContent(int index, T content);
    }
}
