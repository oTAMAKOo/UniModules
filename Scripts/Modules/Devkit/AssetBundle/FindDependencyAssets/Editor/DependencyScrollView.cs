
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions.Devkit;
using Modules.ObjectCache;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundles
{
    public sealed class DependencyScrollView : EditorGUIFastScrollView<FindDependencyAssetsWindow.AssetBundleInfo>
    {
        private ObjectCache<Object> assetCache = null;

        private EditorLayoutTools.TitleGUIStyle assetTitleGuiStyle = null;
        private EditorLayoutTools.TitleGUIStyle dependentTitleGuiStyle = null;

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
                        foreach (var assetPath in content.Assets)
                        {
                            var asset = GetAsset(assetPath);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (assetTitleGuiStyle == null)
                                {
                                    assetTitleGuiStyle = new EditorLayoutTools.TitleGUIStyle
                                    {
                                        backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.3f),
                                        alignment = TextAnchor.MiddleCenter,
                                        fontStyle = FontStyle.Bold,
                                        width = 90f,
                                    };
                                }

                                EditorLayoutTools.Title("Asset", assetTitleGuiStyle);

                                EditorGUILayout.ObjectField("", asset, typeof(Object), false, GUILayout.Width(250f));
                            }
                        }
                    }

                    if (content.DependentAssets.Any())
                    {
                        foreach (var dependentAssetPath in content.DependentAssets)
                        {
                            var dependentAsset = GetAsset(dependentAssetPath);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (dependentTitleGuiStyle == null)
                                {
                                    dependentTitleGuiStyle = new EditorLayoutTools.TitleGUIStyle
                                    {
                                        backgroundColor = new Color(0.4f, 0.4f, 0.9f, 0.5f),
                                        alignment = TextAnchor.MiddleCenter,
                                        fontStyle = FontStyle.Bold,
                                        width = 90f,
                                    };
                                }

                                EditorLayoutTools.Title("Dependent", dependentTitleGuiStyle);

                                EditorGUILayout.ObjectField("", dependentAsset, typeof(Object), false, GUILayout.Width(250f));
                            }
                        }
                    }
                }
            }
        }

        private Object GetAsset(string assetPath)
        {
            if (assetCache == null)
            {
                assetCache = new ObjectCache<Object>();
            }

            Object asset = null;

            if (assetCache.HasCache(assetPath))
            {
                asset = assetCache.Get(assetPath);
            }
            else
            {
                asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                assetCache.Add(assetPath, asset);
            }

            return asset;
        }
    }
}
