
#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.GameText.Components;

namespace Modules.GameText
{
    public sealed partial class GameText
    {
        //----- params -----

        //----- field -----
        
        private Dictionary<string, string> extendTextContents = null;

        //----- property -----

        //----- method -----

        public override string FindText(string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return string.Empty; }

            textGuid = textGuid.Trim();

            var text = base.FindText(textGuid);

            // 内包テキストデータに存在しない場合拡張テキストを検索.
            if (extendTextContents != null)
            {
                if (string.IsNullOrEmpty(text))
                {
                    text = extendTextContents.GetValueOrDefault(textGuid);
                }
            }

            return text;
        }

        private void LoadExtend(GameTextAsset asset)
        {
            extendTextContents = null;

            if (asset == null) { return; }

            var cryptoKey = GetCryptoKey();

            extendTextContents = asset.Contents.ToDictionary(x => x.Guid, x => x.Text.Decrypt(cryptoKey));
        }
    }
}

#endif
