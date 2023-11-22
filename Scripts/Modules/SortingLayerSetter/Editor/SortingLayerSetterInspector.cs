
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.SortingLayerSetter
{
    [CustomEditor(typeof(SortingLayerSetter))]
    public sealed class SortingLayerSetterInspector : ScriptlessEditor
    {
        private SortingLayerSetter instance = null;

        public override void OnInspectorGUI()
        {
            instance = target as SortingLayerSetter;

            DrawInspector();
        }

        private void DrawInspector()
        {
            EditorGUI.BeginChangeCheck();

            var allSortingLayers = SortingLayer.layers;

            var index = allSortingLayers.IndexOf(x => x.value == instance.SortingLayer);
            var labels = allSortingLayers.Select(x => x.name).ToArray();

            index = EditorGUILayout.Popup("SortingLayer", index, labels);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                Reflection.InvokePrivateMethod(instance, "RunCollectContents");
                instance.SortingLayer = allSortingLayers[index].value;
            }

            DrawDefaultScriptlessInspector();

            EditorGUILayout.Separator();

            if (GUILayout.Button("Apply", GUILayout.Width(100f)))
            {
                instance.SetSortingLayer();
            }
        }
    }
}
