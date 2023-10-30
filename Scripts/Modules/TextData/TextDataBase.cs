
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Extensions;
using UniRx;

namespace Modules.TextData.Components
{
    public enum ContentType
    {
        /// <summary> 内蔵 </summary>
        Embedded,

        /// <summary> 配信 </summary>
        Distribution,
    }

    public sealed class TextInfo
    {
        public string identifier = null;

        public string categoryGuid = null;
        
        public string textGuid = null;
        
        public string text = null;
        
        public bool encrypt = false;
    }

    public abstract class TextDataBase<T> : Singleton<T> where T : TextDataBase<T>
    {
        //----- params -----

        //----- field -----

        protected AesCryptoKey cryptoKey = null;

        protected Dictionary<string, TextInfo> texts = null;

        private Dictionary<string, TextInfo> textInfoByIdentifier = null;

        protected Subject<Unit> onUpdateContents = null;

        //----- property -----

        public IReadOnlyDictionary<string, TextInfo> Texts { get { return texts; } }

        /// <summary> アセットファイルフォルダパス </summary>
        public string AssetFolderLocalPath { get; set; } = "TextData";

        //----- method -----

        protected override void OnCreate()
        {
            texts = new Dictionary<string, TextInfo>();

            BuildGenerateContents();

            OnUpdateContentsAsObservable()
                .Subscribe(_ => BuildCache())
                .AddTo(Disposable);
        }

        public virtual string FindTextByTextGuid(string textGuid)
        {
            var info = FindTextInfoByTextGuid(textGuid);

            return info == null ? string.Empty : info.text;
        }

        public TextInfo FindTextInfoByTextGuid(string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return null; }

            textGuid = textGuid.Trim();

            var info = texts.GetValueOrDefault(textGuid);

            if (info == null) { return null; }

            DecryptTextInfo(info);

            return info;
        }

        public string FindTextByIdentifier(string identifier)
        {
            var info = FindTextInfoByIdentifier(identifier);

            return info == null ? string.Empty : info.text;
        }

        public TextInfo FindTextInfoByIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { return null; }

            identifier = identifier.Trim().Encrypt(cryptoKey);

            var info = textInfoByIdentifier.GetValueOrDefault(identifier);

            if (info == null) { return null; }

            DecryptTextInfo(info);

            return info;
        }

        private void DecryptTextInfo(TextInfo info)
        {
            // 復号化していない状態の場合は復号化.
            if (!info.encrypt) { return; }

            info.identifier = info.identifier.Decrypt(cryptoKey);
            info.text = info.text.Decrypt(cryptoKey);
            info.encrypt = false;
        }

        private void BuildCache()
        {
            textInfoByIdentifier = new Dictionary<string, TextInfo>();

            foreach (var info in texts.Values)
            {
                if (info == null) { continue; }

                if (string.IsNullOrEmpty(info.identifier))
                {
                    DecryptTextInfo(info);

                    Debug.LogErrorFormat($"Missing identifier : {info.text}");

                    continue;
                }

                var key = string.Empty;

                if (info.encrypt)
                {
                    key = info.identifier;
                }
                else
                {
                    key = info.identifier.Encrypt(cryptoKey);
                }

                if (!string.IsNullOrEmpty(key))
                {
                    textInfoByIdentifier[key] = info;
                }
            }
        }

        public void SetCryptoKey(string key, string iv)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv)){ return; }

            cryptoKey = new AesCryptoKey(key, iv);
        }

        /// <summary> テキスト更新イベント. </summary>
        public IObservable<Unit> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<Unit>());
        }

        public virtual string FindTextGuid(Enum textType) { return null; }

        protected virtual void BuildGenerateContents() { }
    }
}
