
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.GameText.Components
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
        public string categoryGuid = null;
        public string textGuid = null;
        public string text = null;
        public bool encrypt = false;
    }

    public abstract class GameTextBase<T> : Singleton<T> where T : GameTextBase<T>
    {
        //----- params -----

        //----- field -----

        protected AesCryptoKey cryptoKey = null;

        protected Dictionary<string, TextInfo> texts = null;

        //----- property -----

        public IReadOnlyDictionary<string, TextInfo> Texts { get { return texts; } }

        //----- method -----

        protected override void OnCreate()
        {
            texts = new Dictionary<string, TextInfo>();
            
            BuildGenerateContents();
        }

        public virtual string FindText(string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return string.Empty; }

            textGuid = textGuid.Trim();

            var info = texts.GetValueOrDefault(textGuid);

            if (info == null) { return string.Empty; }

            // 復号化していない状態の場合は復号化.
            if (info.encrypt)
            {
                info.text = info.text.Decrypt(cryptoKey);
                info.encrypt = false;
            }

            return info.text;
        }

        public void SetCryptoKey(string key, string iv)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv)){ return; }

            cryptoKey = new AesCryptoKey(key, iv);
        }

        public virtual string GetAssetFolderName() { return string.Empty; }

        public virtual string FindTextGuid(Enum textType) { return null; }

        protected virtual void BuildGenerateContents() { }
    }
}
