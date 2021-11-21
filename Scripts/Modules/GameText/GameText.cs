
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using Modules.GameText.Components;
using UniRx;

namespace Modules.GameText
{
    public sealed partial class GameText : GameTextBase<GameText>
    {
        //----- params -----

        private const string AssetFileName = "GameText";

        //----- field -----

        private static AesCryptoKey aesCryptoKey = null;

        private Subject<Unit> onUpdateContents = null;

        //----- property -----
        
        //----- method -----

        private GameText() { }

        public AesCryptoKey GetCryptoKey()
        {
            return aesCryptoKey ?? (aesCryptoKey = GetCryptKey());
        }

        /// <summary> 内蔵テキストを読み込み </summary>
        public void LoadEmbedded(string resourcesPath)
        {
            var path = PathUtility.GetPathWithoutExtension(resourcesPath);

            var asset = Resources.Load<GameTextAsset>(path);

            if (asset == null) { return; }

            Clear();

            AddContents(asset);
        }

        /// <summary> 追加でテキストを取り込み </summary>
        public void AddContents(GameTextAsset asset)
        {
            if (asset == null) { return; }
            
            foreach (var categoriesContent in asset.Contents)
            {
                foreach (var textContent in categoriesContent.Texts)
                {
                    var content = new TextInfo()
                    {
                        categoryGuid = categoriesContent.Guid,
                        textGuid = textContent.Guid,
                        text = textContent.Text,
                        encrypt = true,
                    };

                    texts[textContent.Guid] = content;
                }
            }

            #if UNITY_EDITOR

            AddEditorContents(asset);

            #endif

            if (onUpdateContents != null)
            {
                onUpdateContents.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            texts.Clear();
        }

        public static string GetAssetFileName(string identifier)
        {
            var fileNameBuilder = new StringBuilder();

            fileNameBuilder.Append(AssetFileName);

            if (!string.IsNullOrEmpty(identifier))
            {
                fileNameBuilder.AppendFormat("-{0}", identifier);
            }

            fileNameBuilder.Append(".asset");
            
            return fileNameBuilder.ToString();
        }

        /// <summary> テキスト更新イベント. </summary>
        public IObservable<Unit> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<Unit>());
        }
    }
}
