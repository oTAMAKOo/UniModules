
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
       
        private Type arrayType = null;
        private Type elementType = null;

        private List<object> elements = null;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private Subject<object> onUpdateElements = null; 

        //----- property -----

        //----- method -----

        public override void OnOpen()
        {
            base.OnOpen();

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
        }

        public void SetContents(Type type, object value)
        {
            this.arrayType = type;
            this.elementType = EditorRecordFieldUtility.GetDisplayType(type);

            var list = new ArrayList();

            if (value != null)
            {
                foreach (var item in (IEnumerable)value)
                {
                    list.Add(item);
                }
            }
            
            elements = list.Cast<object>().ToList();
        }

        public override void OnGUI(Rect rect)
        {
            if (elements == null) { return; }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(12f)))
            {
                if (MasterController.CanEdit)
                {
                    if (GUILayout.Button(toolbarPlusIcon, EditorStyles.toolbarButton, GUILayout.Width(25f)))
                    {
                        elements.Add(elementType.GetDefaultValue());

                        OnUpdateElements();
                    }
                }
            }

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                var removeIndexs = new List<int>();

                for (var i = 0; i < elements.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        var fieldRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                        elements[i] = EditorRecordFieldUtility.DrawField(fieldRect, elements[i], elementType);

                        if (EditorGUI.EndChangeCheck() && MasterController.CanEdit)
                        {
                            OnUpdateElements();
                        }

                        if (MasterController.CanEdit)
                        {
                            if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(25f)))
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
            var arraySize = elements.Count;          

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
            else if (arrayType.GetGenericTypeDefinition() == typeof(IList<>))
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
