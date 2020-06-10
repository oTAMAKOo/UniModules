﻿﻿
using UnityEngine;
using UnityEditor;

namespace Modules.SortingLayerSetter
{
    [CustomEditor(typeof(SortingLayerSetter))]
    public sealed class SortingLayerSetterInspector : UnityEditor.Editor
    {
        private SortingLayerSetter instance = null;

        public override void OnInspectorGUI()
        {
            instance = target as SortingLayerSetter;

            base.OnInspectorGUI();

            DrawInspector();
        }

        private void DrawInspector()
        {
            GUILayout.Space(10f);

            if (GUILayout.Button("Apply", GUILayout.Width(100f)))
            {
                instance.SetSortingLayer();
            }
        }
    }
}
