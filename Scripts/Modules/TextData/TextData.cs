
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using Modules.TextData.Components;
using UniRx;

namespace Modules.TextData
{
    public sealed partial class TextData : TextDataBase<TextData>
    {
        //----- params -----

        private const string AssetFileName = "TextData";

        //----- field -----
        
        private Subject<Unit> onUpdateContents = null;

        //----- property -----
        
        //----- method -----

        private TextData() { }

        /// <summary> 内蔵テキストを読み込み </summary>
        public void LoadEmbedded(string resourcesPath)
        {
            var path = PathUtility.GetPathWithoutExtension(resourcesPath);

            var asset = Resources.Load<TextDataAsset>(path);
            
            LoadEmbedded(asset);
        }

        /// <summary> 内蔵テキストを読み込み </summary>
        public void LoadEmbedded(TextDataAsset textDataAsset)
        {
            if (textDataAsset == null) { return; }

            Clear();

            AddContents(textDataAsset);
        }

        /// <summary> 追加でテキストを取り込み </summary>
        public void AddContents(TextDataAsset asset)
        {
            if (asset == null) { return; }
            
            foreach (var categoriesContent in asset.Contents)
            {
                foreach (var textContent in categoriesContent.Texts)
                {
                    var content = new TextInfo()
                    {
                        textIdentifier = $"{categoriesContent.Name}-{textContent.EnumName}",
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

            BuildContents();

            if (onUpdateContents != null)
            {
                onUpdateContents.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            if (textGuidByTextIdentifier != null)
            {
                textGuidByTextIdentifier.Clear();
            }

            if (texts != null)
            {
                texts.Clear();
            }
        }

        public static string Get(string identifier)
        {
            return Instance.FindTextByIdentifier(identifier);
        }

        public static string Format(string identifier, params object[] args)
        {
            return string.Format(Instance.FindTextByIdentifier(identifier), args);
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
