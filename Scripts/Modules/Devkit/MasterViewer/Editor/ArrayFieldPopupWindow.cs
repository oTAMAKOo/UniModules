
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
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

        private MasterController masterController = null;

        private Type arrayType = null;
        private Type displayType = null;

        private List<object> elements = null;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private Subject<object> onUpdateElements = null; 

        //----- property -----

        //----- method -----

        public ArrayFieldPopupWindow(MasterController masterController)
        {
            this.masterController = masterController;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
        }

        public void SetContents(Type type, object[] elements)
        {
            this.arrayType = type;
            this.displayType = EditorRecordFieldUtility.GetDisplayType(type);
            this.elements = elements.ToList();
        }

        public override void OnGUI(Rect rect)
        {
            if (elements == null) { return; }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                if (masterController.EnableEdit)
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

                        if (masterController.EnableEdit)
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
            if (!masterController.EnableEdit) { return; }

            var arraySize = elements.Count;

            var elementType = EditorRecordFieldUtility.GetDisplayType(arrayType);

            object value = null;

            if (arrayType.IsArray)
            {
                var array = Array.CreateInstance(elementType, arraySize);

                for (var i = 0; i < arraySize; i++)
                {
                    var val = Convert.ChangeType(elements[i], elementType);

                    array.SetValue(val, i);
                }

                value = array;
            }

            if (arrayType.GetGenericTypeDefinition() == typeof(IList<>))
            {
                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(elementType);

                var list = (IList)Activator.CreateInstance(constructedListType);

                for (var i = 0; i < elements.Count; i++)
                {
                    list.Add(elements[i]);
                }

                value = list;
            }

            if (onUpdateElements != null)
            {
                onUpdateElements.OnNext(value);
            }
        }

        public IObservable<object> OnUpdateElementsAsObservable()
        {
            return onUpdateElements ?? (onUpdateElements = new Subject<object>());
        }
    }
}
