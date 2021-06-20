
using UnityEngine;
using UnityEditor;
using Extensions.Devkit;
using Modules.ObjectCache;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundles
{
    public sealed class ReferenceScrollView : EditorGUIFastScrollView<FindDependencyAssetsWindow.AssetReferenceInfo>
    {
        private ObjectCache<Object> assetCache = null;

        private EditorLayoutTools.TitleGUIStyle titleGuiStyle = null;

        public override Direction Type
        {
            get { return Direction.Vertical; }
        }

        protected override void DrawContent(int index, FindDependencyAssetsWindow.AssetReferenceInfo content)
        {
            var count = content.ReferenceAssets.Length;

            if (count < 2) { return; }

            var asset = GetAsset(content.Asset);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (titleGuiStyle == null)
                {
                    titleGuiStyle = new EditorLayoutTools.TitleGUIStyle
                    {
                        backgroundColor = new Color(1f, 0.65f, 0f, 0.5f),
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        width = 30f,
                    };
                }

                EditorLayoutTools.Title(count.ToString(), titleGuiStyle);
                EditorGUILayout.ObjectField("", asset, typeof(Object), false, GUILayout.Width(250f));
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
