
using System;
using System.Collections.Generic;
using Extensions;

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
        public string textIdentifier = null;

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

        protected Dictionary<string, string> textGuidByTextIdentifier = null;

        //----- property -----

        public IReadOnlyDictionary<string, TextInfo> Texts { get { return texts; } }

        /// <summary> アセットファイルフォルダパス </summary>
        public string AssetFolderLocalPath { get; set; } = "TextData";

        //----- method -----

        protected override void OnCreate()
        {
            texts = new Dictionary<string, TextInfo>();
            
            BuildGenerateContents();
        }

        public virtual string FindText(string textGuid)
        {
            var info = FindTextInfo(textGuid);

            if (info == null) { return null; }

            return info.text;
        }

        public TextInfo FindTextInfo(string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return null; }

            textGuid = textGuid.Trim();

            var info = texts.GetValueOrDefault(textGuid);

            if (info == null) { return null; }

            // 復号化していない状態の場合は復号化.
            if (info.encrypt)
            {
                info.text = info.text.Decrypt(cryptoKey);
                info.encrypt = false;
            }

            return info;
        }

        public string FindTextByIdentifier(string textIdentifier)
        {
            if (string.IsNullOrEmpty(textIdentifier)) { return null; }

            if (textGuidByTextIdentifier == null) { return null; }

            var textGuid = textGuidByTextIdentifier.GetValueOrDefault(textIdentifier);

            if (textGuid == null) { return null; }

            return FindText(textGuid);
        }

        public void SetCryptoKey(string key, string iv)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv)){ return; }

            cryptoKey = new AesCryptoKey(key, iv);
        }

        protected void BuildContents()
        {
            textGuidByTextIdentifier = new Dictionary<string, string>();

            foreach (var info in texts.Values)
            {
                textGuidByTextIdentifier[info.textIdentifier] = info.textGuid;
            }
        }

        public virtual string FindTextGuid(Enum textType) { return null; }

        protected virtual void BuildGenerateContents() { }
    }
}
