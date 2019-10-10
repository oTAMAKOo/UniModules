
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
            private GUIContent toolbarPlusIcon = null;
            private Func<int, T, T> drawContent = null;

            public override Direction Type { get { return Direction.Vertical; } }

            public FastScrollView(RegisterScrollView<T> instance, Func<int, T, T> drawContent)
            {
                this.instance = instance;
                this.drawContent = drawContent;

                toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
            }

            protected override void DrawContent(int index, T content)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    if (drawContent != null)
                    {
                        content = drawContent(index, content);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Contents[index] = content;

                        if (instance.onUpdateContents != null)
                        {
                            instance.onUpdateContents.OnNext(Contents);
                        }
                    }

                    if (GUILayout.Button(toolbarPlusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                    {
                        var list = Contents.ToList();

                        list.RemoveAt(index);

                        Contents = list.ToArray();

                        UpdateContens();
                    }
                }
            }

            protected void UpdateContens()
            {
                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(Contents);
                }

                RequestRepaint();
            }

            public IObservable<T[]> OnUpdateContentsAsObservable()
            {
                return onUpdateContents ?? (onUpdateContents = new Subject<T[]>());
            }
        }

        //----- field -----

        private Func<T> createContent = null;

        private FastScrollView scrollView = null;
        private GUIContent toolbarPlusIcon = null;

        private Subject<T[]> onUpdateContents = null;
        private Subject<Unit> onRepaintRequest = null;

        //----- property -----

        public T[] Contents
        {
            get { return scrollView.Contents; }
            protected set { scrollView.Contents = value; }
        }

        //----- method -----

        public RegisterScrollView(Func<T> createContent, Func<int, T, T> drawContent)
        {
            this.createContent = createContent;

            scrollView = new FastScrollView(this, drawContent);

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");

            scrollView.OnUpdateContentsAsObservable()
                .Subscribe(x =>
                    {
                        if (onUpdateContents != null)
                        {
                            onUpdateContents.OnNext(Contents);
                        }
                    })
                .AddTo(Disposable);

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

        public void DrawGUI(params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(toolbarPlusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                {
                    var list = Contents.ToList();

                    list.Add(createContent());
                }
            }

            scrollView.Draw(true, options);
        }

        public IObservable<T[]> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<T[]>());
        }
    }
}
