
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.AssetBundles
{
    public class ReferenceScrollView : EditorGUIFastScrollView<FindDependencyAssetsWindow.AssetReferenceInfo>
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
                var backgroundColor = new Color(1f, 0.65f, 0f, 0.5f);

                EditorLayoutTools.DrawLabelWithBackground(count.ToString(), backgroundColor, null, TextAnchor.MiddleCenter, FontStyle.Bold, 30f);
                EditorGUILayout.ObjectField("", content.Asset, typeof(Object), false, GUILayout.Width(250f));
            }
        }
    }
}
