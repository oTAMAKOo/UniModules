
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Extensions.Devkit;

namespace Modules.Particle
{
    [CustomEditor(typeof(ParticlePlayerSortingOrder), true)]
    public class ParticlePlayerSortingOrderInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ParticlePlayer particlePlayer = null;

        private ParticlePlayerSortingOrder instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as ParticlePlayerSortingOrder;

            particlePlayer = instance.FindParentParticlePlayer();
        }

        public override void OnInspectorGUI()
        {
            instance = target as ParticlePlayerSortingOrder;

            var originLabelWidth = EditorLayoutTools.SetLabelWidth(120f);

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(5f);

                    EditorGUILayout.PrefixLabel("SortingOrder");
                }

                using (new ContentsScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var text = string.Empty;
                        var size = Vector2.zero;
                        
                        var style = GUI.skin.box;
                        style.alignment = TextAnchor.MiddleLeft;

                        text = string.Format("{0} +", particlePlayer.SortingOrder);
                        size = style.CalcSize(new GUIContent(text));

                        EditorGUILayout.LabelField(text, GUILayout.Width(size.x));

                        EditorGUI.BeginChangeCheck();

                        text = instance.SortingOrder.ToString();
                        size = style.CalcSize(new GUIContent(text));

                        var width = 30f < size.x ? size.x : 30f;
                        var sortingOrder = EditorGUILayout.DelayedIntField(instance.SortingOrder, GUILayout.Width(width + 10f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            instance.Set(sortingOrder);
                        }

                        var currentSortingOrder = particlePlayer.SortingOrder + instance.SortingOrder;

                        text = string.Format("= {0}", currentSortingOrder);
                        size = style.CalcSize(new GUIContent(text));

                        EditorGUILayout.LabelField(text, GUILayout.Width(size.x));
                    }
                }
            }

            EditorLayoutTools.SetLabelWidth(originLabelWidth);
        }
    }
}
