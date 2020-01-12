
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using UniRx;

namespace Modules.Devkit.MasterViewer
{
    public sealed class ArrayFieldPopupWindow : PopupWindowContent
    {
        //----- params -----

        //----- field -----

        private Type arrayType = null;
        private Type displayType = null;

        private List<object> elements = null;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private Subject<object> onUpdateElements = null; 

        //----- property -----

        public bool EnableEdit { get; set; }

        //----- method -----

        public override void OnOpen()
        {
            base.OnOpen();

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
        }

        public void SetContents(Type type, object[] elements)
        {
            this.arrayType = type;
            this.displayType = EditorRecordFieldUtility.GetDisplayType(type.GetElementType());
            this.elements = elements.ToList();
        }

        public override void OnGUI(Rect rect)
        {
            if (elements == null) { return; }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                if (EnableEdit)
                {
                    if (GUILayout.Button(toolbarPlusIcon, EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    {
                        elements.Add(displayType.GetDefaultValue());

                        OnUpdateElements();
                    }
                }

                GUILayout.FlexibleSpace();
            }

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                var removeIndexs = new List<int>();

                for (var i = 0; i < elements.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        elements[i] = EditorRecordFieldUtility.DrawRecordField(elements[i], displayType);

                        if (EditorGUI.EndChangeCheck())
                        {
                            OnUpdateElements();
                        }

                        if (EnableEdit)
                        {
                            if (GUILayout.Button(toolbarMinusIcon, GUILayout.Width(20f)))
                            {
                                removeIndexs.Add(i);
                            }
                        }
                    }
                }

                if (removeIndexs.Any())
                {
                    foreach (var removeIndex in removeIndexs)
                    {
                        elements.RemoveAt(removeIndex);
                    }

                    OnUpdateElements();
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private void OnUpdateElements()
        {
            if (!EnableEdit) { return; }

            var arraySize = elements.Count;

            var elementType = arrayType.GetElementType();

            var array = Array.CreateInstance(elementType, arraySize);

            for (var i = 0; i < arraySize; i++)
            {
                var val = Convert.ChangeType(elements[i], elementType);

                array.SetValue(val, i);
            }

            if (onUpdateElements != null)
            {
                onUpdateElements.OnNext(array);
            }
        }

        public IObservable<object> OnUpdateElementsAsObservable()
        {
            return onUpdateElements ?? (onUpdateElements = new Subject<object>());
        }
    }
}
