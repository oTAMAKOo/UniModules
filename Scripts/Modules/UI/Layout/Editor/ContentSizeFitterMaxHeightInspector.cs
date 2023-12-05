
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Layout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof (ContentSizeFitterMaxHeight))]
    public sealed class ContentSizeFitterMaxHeightInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var instance = target as ContentSizeFitterMaxHeight;

            var maxHeight = Reflection.GetPrivateField<ContentSizeFitterMaxHeight, float>(instance, "maxHeight");

            EditorGUI.BeginChangeCheck();

            maxHeight = EditorGUILayout.DelayedFloatField("MaxHeight", maxHeight);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);
                Reflection.SetPrivateField(instance, "maxHeight", maxHeight);
            }
        }
    }
}
