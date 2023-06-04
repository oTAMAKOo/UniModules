
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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

        protected ReorderableList reorderableList = null;

        private Subject<T[]> onUpdateContents = null;

        //----- property -----

        public IReadOnlyList<T> Contents
        {
            get { return contents; }
        }

        //----- method -----

        public RegisterScrollView()
        {
            SetupReorderableList();
        }

        private void SetupReorderableList()
        {
            reorderableList = new ReorderableList(new List<T>(), typeof(T));

            // ヘッダーは描画しない.
            reorderableList.headerHeight = 0;
            reorderableList.drawHeaderCallback = r => {};

            // 要素描画コールバック.
            reorderableList.drawElementCallback = (r, index, isActive, isFocused) => 
            {
                r.position = Vector.SetY(r.position, r.position.y + 2f);
                r.height = EditorGUIUtility.singleLineHeight;

                var content = contents.ElementAtOrDefault(index);

                EditorGUI.BeginChangeCheck();

                content = DrawContent(r, index, content);

                if (EditorGUI.EndChangeCheck())
                {
                    contents[index] = content;

                    ValidateContent(content);

                    reorderableList.list = contents;

                    if (onUpdateContents != null)
                    {
                        onUpdateContents.OnNext(contents.ToArray());
                    }
                }
            };

            // 順番入れ替えコールバック.
            reorderableList.onReorderCallback = list =>
            {
                contents = list.list.Cast<T>().ToList();

                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(contents.ToArray());
                }
            };

            // 追加コールバック.
            reorderableList.onAddCallback = list =>
            {
                contents.Add(CreateNewContent());

                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(contents.ToArray());
                }
            };

            // 削除コールバック.
            reorderableList.onRemoveCallback = list =>
            {
                contents.RemoveAt(list.index);

                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(contents.ToArray());
                }
            };
        }

        public void SetContents(T[] contents)
        {
            this.contents = contents.ToList();

            reorderableList.list = this.contents;
        }

        public virtual void DrawGUI()
        {
            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawHeaderContent();

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(2f);

            reorderableList.DoLayoutList();

            GUILayout.Space(2f);
        }

        public IObservable<T[]> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<T[]>());
        }

        protected virtual void DrawHeaderContent() { }

        protected virtual void ValidateContent(T content) { }

        protected abstract T CreateNewContent();

        protected abstract T DrawContent(Rect rect, int index, T content);
    }
}
