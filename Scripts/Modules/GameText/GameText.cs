
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;
using Modules.GameText.Components;
using UniRx;

namespace Modules.GameText
{
    public sealed partial class GameText : GameTextBase<GameText>
    {
        //----- params -----

        public enum AssetType
        {
            BuiltIn,
            Update,
            Extend,
        }

        private const string AssetFileName = "GameText";

        //----- field -----

        private AesManaged aesManaged = null;

        private long? builtInAssetUpdateAt = null;

        private Subject<Unit> onUpdateContents = null;

        //----- property -----
        
        //----- method -----

        private GameText(){ }

        public AesManaged GetAesManaged()
        {
            return aesManaged ?? (aesManaged = AESExtension.CreateAesManaged(GetAesKey(), GetAesIv()));
        }

        /// <summary> 内蔵テキストを読み込み </summary>
        public void LoadBuiltInAsset(string resourcesPath)
        {
            var path = PathUtility.GetPathWithoutExtension(resourcesPath);

            var asset = Resources.Load<GameTextAsset>(path);

            if (asset == null) { return; }

            Clear();

            var contents = asset.Contents.ToArray();

            var aesManaged = GetAesManaged();

            cache = contents.ToDictionary(x => x.Guid, x => x.Text.Decrypt(aesManaged));

            builtInAssetUpdateAt = asset.UpdateAt;

            if (onUpdateContents != null)
            {
                onUpdateContents.OnNext(Unit.Default);
            }
        }

        /// <summary> 追加でテキストを取り込み </summary>
        public void ImportAsset(GameTextAsset asset, bool force = false)
        {
            if (asset == null) { return; }
            
            // 内蔵テキストを読み込んでいない時は追加取り込みさせない.
            if (!builtInAssetUpdateAt.HasValue) { return; }

            if (!force)
            {
                // 生成日時がない時は取り込まない.
                if (!asset.UpdateAt.HasValue) { return; }

                // 内蔵テキストより古いテキストは取り込まない.
                if (asset.UpdateAt.Value < builtInAssetUpdateAt.Value) { return; }
            }

            var contents = asset.Contents.ToArray();

            var aesManaged = GetAesManaged();

            var textContents = contents.ToDictionary(x => x.Guid, x => x.Text.Decrypt(aesManaged));

            foreach (var textContent in textContents)
            {
                // 更新.
                if (cache.ContainsKey(textContent.Key))
                {
                    cache[textContent.Key] = textContent.Value;
                }
                // 追加.
                else
                {
                    cache.Add(textContent.Key, textContent.Value);
                }
            }

            if (onUpdateContents != null)
            {
                onUpdateContents.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            cache.Clear();

            builtInAssetUpdateAt = null;
        }

        public static string GetAssetFileName(AssetType assetType, string identifier)
        {
            var identifierStr = string.Empty;

            if (!string.IsNullOrEmpty(identifier))
            {
                identifierStr = "-" + identifier;
            }
            
            return string.Format("{0}-{1}{2}.asset", AssetFileName, assetType.ToString(), identifierStr);
        }

        /// <summary> テキスト更新イベント. </summary>
        public IObservable<Unit> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<Unit>());
        }
    }
}
