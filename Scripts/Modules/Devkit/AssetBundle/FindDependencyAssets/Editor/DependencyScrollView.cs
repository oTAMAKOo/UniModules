
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.AssetBundles
{
    public sealed class DependencyScrollView : EditorGUIFastScrollView<FindDependencyAssetsWindow.AssetBundleInfo>
    {
        public override Direction Type
        {
            get { return Direction.Vertical; }
        }

        protected override void DrawContent(int index, FindDependencyAssetsWindow.AssetBundleInfo content)
        {
            content.IsOpen = EditorLayoutTools.Header(content.AssetBundleName, content.IsOpen);

            if (content.IsOpen)
            {
                using (new ContentsScope())
                {
                    if (content.Assets.Any())
                    {
                        foreach (var asset in content.Assets)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var titleStyle = new EditorLayoutTools.TitleGUIStyle
                                {
                                    backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.3f),
                                    alignment = TextAnchor.MiddleCenter,
                                    fontStyle = FontStyle.Bold,
                                    width = 90f,
                                };

                                EditorLayoutTools.Title("Asset", titleStyle);

                                EditorGUILayout.ObjectField("", asset, typeof(Object), false, GUILayout.Width(250f));
                            }
                        }
                    }

                    if (content.DependentAssets.Any())
                    {
                        foreach (var dependentAsset in content.DependentAssets)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var titleStyle = new EditorLayoutTools.TitleGUIStyle
                                {
                                    backgroundColor = new Color(0.4f, 0.4f, 0.9f, 0.5f),
                                    alignment = TextAnchor.MiddleCenter,
                                    fontStyle = FontStyle.Bold,
                                    width = 90f,
                                };

                                EditorLayoutTools.Title("Dependent", titleStyle);

                                EditorGUILayout.ObjectField("", dependentAsset, typeof(Object), false, GUILayout.Width(250f));
                            }
                        }
                    }
                }
            }
        }
    }
}
