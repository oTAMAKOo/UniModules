
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextSpacing))]
    public sealed class TextSpacingInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Font currentFont = null;
        private FontKerningSetting[] settings = null;

        private TextSpacing instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as TextSpacing;

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            var spacing = EditorGUILayout.FloatField("Tracking", instance.Tracking);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);
                instance.Tracking = spacing;
            }
            
            if (currentFont != instance.Text.font)
            {
                currentFont = instance.Text.font;

                var assets = UnityEditorUtility.FindAssetsByType<FontKerningSetting>("t:FontKerningSetting");

                settings = assets.Where(x => x.Font == currentFont).ToArray();
            }

            using (new DisableScope(settings.IsEmpty()))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var labels = settings.Select(x => x.name).ToList();

                    labels.Insert(0, "None");

                    var index = settings.IndexOf(x => x == instance.KerningSetting);
                    var selection = index != -1 ? index + 1 : 0; 

                    EditorGUI.BeginChangeCheck();

                    selection = EditorGUILayout.Popup("Kerning", selection, labels.ToArray());

                    if (EditorGUI.EndChangeCheck())
                    {
                        var kerningSetting = 0 < selection ? settings.ElementAtOrDefault(selection - 1) : null;

                        UnityEditorUtility.RegisterUndo(instance);

                        instance.KerningSetting = kerningSetting;
                    }

                    using (new DisableScope(instance.KerningSetting == null))
                    {
                        if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(45f)))
                        {
                            Selection.activeObject = instance.KerningSetting;
                        }
                    }
                }
            }

            if (settings.IsEmpty())
            {
                EditorGUILayout.HelpBox("Can not find kerning setting file for this font.", MessageType.Info);
            }
        }
    }
}
