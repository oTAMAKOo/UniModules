
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        //----- field -----

        private static GUIStyle dragAndDropTextStyle = null;

        //----- property -----

        //----- method -----
        
        public static T DragAndDrop<T>(string text, float widthMin = 0, float? height = null) where T : Object 
        {
            if (!height.HasValue)
            {
                height = EditorGUIUtility.singleLineHeight;
            }

            var objectReferences = DragAndDropObjects(text, widthMin, height.Value);

            if (objectReferences == null){ return null; }

            return objectReferences.FirstOrDefault(x => x is T) as T;
        }
        
        public static T[] MultipleDragAndDrop<T>(string text, float widthMin = 0, float? height = null) where T : Object 
        {
            if (!height.HasValue)
            {
                height = EditorGUIUtility.singleLineHeight;
            }

            var objectReferences = DragAndDropObjects(text, widthMin, height.Value);
            
            if (objectReferences == null || objectReferences.IsEmpty()) { return new T[0]; }
            
            return objectReferences.OfType<T>().ToArray();
        }

        private static Object[] DragAndDropObjects(string text, float widthMin, float height, Color? textColor = null)
        {
            var dropArea = GUILayoutUtility.GetRect(widthMin, height, GUILayout.ExpandWidth(true));
    
            if (dragAndDropTextStyle == null)
            {
                dragAndDropTextStyle = new GUIStyle(GUI.skin.box);

                dragAndDropTextStyle.alignment = TextAnchor.MiddleCenter;
                dragAndDropTextStyle.normal.textColor = textColor ?? Color.gray;
            }

            GUI.Box(dropArea, text, dragAndDropTextStyle);

            var currentEvent = Event.current;
            
            if (!dropArea.Contains (currentEvent.mousePosition)) { return null; }
            
            var eventType = currentEvent.type;
            
            if(eventType != EventType.DragUpdated && eventType != EventType.DragPerform) { return null; }

            UnityEditor.DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if(eventType != EventType.DragPerform){ return null; }

            UnityEditor.DragAndDrop.AcceptDrag();
            
            currentEvent.Use();

            return UnityEditor.DragAndDrop.objectReferences;
        }
    }
}