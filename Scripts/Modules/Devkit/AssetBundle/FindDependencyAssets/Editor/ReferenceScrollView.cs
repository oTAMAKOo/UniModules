
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.AssetBundles
{
    public sealed class ReferenceScrollView : EditorGUIFastScrollView<FindDependencyAssetsWindow.AssetReferenceInfo>
    {
        public override Direction Type
        {
            get { return Direction.Vertical; }
        }

        protected override void DrawContent(int index, FindDependencyAssetsWindow.AssetReferenceInfo content)
        {
            var count = content.ReferenceAssets.Length;

            if (count < 2) { return; }

            using (new EditorGUILayout.HorizontalScope())
            {
                var titleStyle = new EditorLayoutTools.TitleGUIStyle
                {
                    backgroundColor = new Color(1f, 0.65f, 0f, 0.5f),
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    width = 30f,
                };

                EditorLayoutTools.Title(count.ToString(), titleStyle);
                EditorGUILayout.ObjectField("", content.Asset, typeof(Object), false, GUILayout.Width(250f));
            }
        }
    }
}
