
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Layout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof (ContentSizeFitterMaxWidth))]
    public sealed class ContentSizeFitterMaxWidthInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var instance = target as ContentSizeFitterMaxWidth;

            var maxWidth = Reflection.GetPrivateField<ContentSizeFitterMaxWidth, float>(instance, "maxWidth");

            EditorGUI.BeginChangeCheck();

            maxWidth = EditorGUILayout.DelayedFloatField("MaxWidth", maxWidth);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);
                Reflection.SetPrivateField(instance, "maxWidth", maxWidth);
            }

            var priority = Reflection.GetPrivateField<ContentSizeFitterMaxWidth, int>(instance, "priority");

            EditorGUI.BeginChangeCheck();

            priority = EditorGUILayout.DelayedIntField("LayoutPriority", priority);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);
                Reflection.SetPrivateField(instance, "priority", priority);
            }
        }
    }
}
