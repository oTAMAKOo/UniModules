
using UnityEngine;
using UnityEditor;
using Extensions;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        //===========================================
        // ObjectField.
        //===========================================

        public static T ObjectField<T>(T obj, bool allowSceneObjects, params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(obj, typeof(T), allowSceneObjects, options) as T;
        }

        public static T ObjectField<T>(string label, T obj, bool allowSceneObjects, params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(T), allowSceneObjects, options) as T;
        }

        //===========================================
        // TextField.
        //===========================================

        public static string TextField(string label, string text, float space = 0f, params GUILayoutOption[] options)
        {
            return TextFieldInternal(label, text, false, space, options);
        }

        public static string DelayedTextField(string label, string text, float space = 0f, params GUILayoutOption[] options)
        {
            return TextFieldInternal(label, text, true, space, options);
        }

        private static string TextFieldInternal(string label, string text, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ?
                EditorGUILayout.DelayedTextField(label, text, options) :
                EditorGUILayout.TextField(label, text, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        //===========================================
        // IntField.
        //===========================================

        public static int IntField(string label, int value, float space = 0f, params GUILayoutOption[] options)
        {
            return IntFieldInternal(label, value, false, space, options);
        }

        public static int DelayedIntField(string label, int value, float space = 0f, params GUILayoutOption[] options)
        {
            return IntFieldInternal(label, value, true, space, options);
        }

        private static int IntFieldInternal(string label, int value, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ?
                EditorGUILayout.DelayedIntField(label, value, options) :
                EditorGUILayout.IntField(label, value, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        public static Vector2Int IntRangeField(string prefix, string leftCaption, string rightCaption, int x, int y, bool editable = true)
        {
            return IntRangeFieldInternal(prefix, leftCaption, rightCaption, x, y, false, editable);
        }

        public static Vector2Int DelayedIntRangeField(string prefix, string leftCaption, string rightCaption, int x, int y, bool editable = true)
        {
            return IntRangeFieldInternal(prefix, leftCaption, rightCaption, x, y, true, editable);
        }

        private static Vector2Int IntRangeFieldInternal(string prefix, string leftCaption, string rightCaption, int x, int y, bool delayed, bool editable = true)
        {
            var retVal = new Vector2Int() { x = x, y = y };

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    GUILayout.Label(prefix, GUILayout.Width(74f));
                }

                var originLabelWidth = SetLabelWidth(48f);

                if (editable)
                {
                    var backgroundColor = GUI.backgroundColor;

                    GUI.backgroundColor = new Color(0f, 0.7f, 1f, 1f); // Blue.

                    retVal.x = delayed ?
                        EditorGUILayout.DelayedIntField(leftCaption, x, GUILayout.MinWidth(30f)) :
                        EditorGUILayout.IntField(leftCaption, x, GUILayout.MinWidth(30f));

                    retVal.y = delayed ?
                        EditorGUILayout.DelayedIntField(rightCaption, y, GUILayout.MinWidth(30f)) :
                        EditorGUILayout.IntField(rightCaption, y, GUILayout.MinWidth(30f));

                    GUI.backgroundColor = backgroundColor;
                }
                else
                {
                    GUILayout.Label(leftCaption + " ", GUILayout.Width(48f));
                    GUILayout.Label(x.ToString(), EditorStyles.textArea, GUILayout.MinWidth(30f));

                    GUILayout.Label(rightCaption + " ", GUILayout.Width(48f));
                    GUILayout.Label(y.ToString(), EditorStyles.textArea, GUILayout.MinWidth(30f));
                }

                SetLabelWidth(originLabelWidth);
            }

            return retVal;
        }

        //===========================================
        // FloatField.
        //===========================================

        public static float FloatField(string label, float value, float space = 0f, params GUILayoutOption[] options)
        {
            return FloatFieldInternal(label, value, false, space, options);
        }

        public static float DelayedFloatField(string label, float value, float space = 0f, params GUILayoutOption[] options)
        {
            return FloatFieldInternal(label, value, true, space, options);
        }

        private static float FloatFieldInternal(string label, float value, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ?
                EditorGUILayout.DelayedFloatField(label, value, options) :
                EditorGUILayout.FloatField(label, value, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        //===========================================
        // BoolField.
        //===========================================

        public static bool BoolField(string label, bool state, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = EditorGUILayout.Toggle(label, state, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }
    }
}
