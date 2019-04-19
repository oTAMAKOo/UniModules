
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextSpacing))]
    public class TextSpacingInspector : UnityEditor.Editor
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
                UnityEditorUtility.RegisterUndo("TextSpacingInspector Undo", instance);
                instance.Tracking = spacing;
            }

            GUILayout.Space(2f);

            if (currentFont != instance.Text.font)
            {
                currentFont = instance.Text.font;

                if (settings == null)
                {
                    var assets = UnityEditorUtility.FindAssetsByType<FontKerningSetting>("t:FontKerningSetting").ToArray();

                    settings = assets.Where(x => x.Font == currentFont).ToArray();
                }
            }

            FontKerningSetting kerningSetting = null;
            
            EditorGUI.BeginChangeCheck();

            kerningSetting = (FontKerningSetting)EditorGUILayout.ObjectField("Font Setting", instance.KerningSetting, typeof(FontKerningSetting), false);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("TextSpacingInspector Undo", instance);
                Reflection.SetPrivateField(instance, "kerningSetting", kerningSetting);
            }

            if (settings.Any())
            {
                GUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUIUtility.labelWidth);

                    var labels = settings.Select(x => x.name).ToArray();
                    var selection = settings.IndexOf(x => x == kerningSetting);

                    EditorGUI.BeginChangeCheck();

                    var index = EditorGUILayout.Popup(selection, labels);

                    if (EditorGUI.EndChangeCheck())
                    {
                        kerningSetting = settings.ElementAtOrDefault(index);

                        UnityEditorUtility.RegisterUndo("TextSpacingInspector Undo", instance);
                        Reflection.SetPrivateField(instance, "kerningSetting", kerningSetting);
                    }
                }
            }

            if (kerningSetting != null && instance.Text.font != kerningSetting.Font)
            {
                EditorGUILayout.HelpBox("Kerning is invalid because the font of the configuration file is different.", MessageType.Warning);
            }
        }
    }
}
