
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.PatternTexture
{
    public sealed partial class DummyPatternImage
    {
        //----- params -----

        //----- field -----

        // 仮表示するアセットの guid.
        [SerializeField, HideInInspector]
        private string assetGuid = null;
        // 仮表示するパターン名のハッシュ.
        [SerializeField, HideInInspector]
        private string patternNameHash = null;

        // パターン名 → ハッシュ.
        private static Dictionary<string, string> patternNameHashCache = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            ApplyDummy();
        }

        void OnDisable()
        {
            ClearDummy();
        }

        private void ApplyDummy()
        {
            if (Application.isPlaying) { return; }

            if (BuildPipeline.isBuildingPlayer) { return; }

            var target = PatternImage;

            if (target == null) { return; }

            var patternTexture = LoadPatternTexture();

            var patternName = FindPatternName(patternTexture);

            if (patternTexture == null || string.IsNullOrEmpty(patternName))
            {
                target.ClearDummy();

                return;
            }

            target.SetDummy(patternTexture, patternName);
        }

        private void ClearDummy()
        {
            if (Application.isPlaying) { return; }

            var target = PatternImage;

            if (target == null) { return; }

            target.ClearDummy();
        }

        private void SetDummyAsset(PatternTexture patternTexture)
        {
            var guid = string.Empty;

            if (patternTexture != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(patternTexture);

                guid = string.IsNullOrEmpty(assetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assetPath);
            }

            assetGuid = guid;

            // 切り替え先に同じパターンが無ければ選択を外す.
            if (string.IsNullOrEmpty(FindPatternName(patternTexture)))
            {
                patternNameHash = string.Empty;
            }

            ApplyDummy();
        }

        private void SetDummyPatternName(string patternName)
        {
            patternNameHash = string.IsNullOrEmpty(patternName) ? string.Empty : GetPatternNameHash(patternName);

            ApplyDummy();
        }

        private PatternTexture LoadPatternTexture()
        {
            if (string.IsNullOrEmpty(assetGuid)) { return null; }

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetPath)) { return null; }

            return AssetDatabase.LoadAssetAtPath<PatternTexture>(assetPath);
        }

        private string FindPatternName(PatternTexture patternTexture)
        {
            if (patternTexture == null) { return null; }

            if (string.IsNullOrEmpty(patternNameHash)) { return null; }

            var patternData = patternTexture.GetAllPatternData()
                .FirstOrDefault(x => GetPatternNameHash(x.TextureName) == patternNameHash);

            return patternData != null ? patternData.TextureName : null;
        }

        private static string GetPatternNameHash(string patternName)
        {
            if (patternNameHashCache == null)
            {
                patternNameHashCache = new Dictionary<string, string>();
            }

            var hash = patternNameHashCache.GetValueOrDefault(patternName);

            if (string.IsNullOrEmpty(hash))
            {
                hash = patternName.GetHash();

                patternNameHashCache[patternName] = hash;
            }

            return hash;
        }
    }
}

#endif
