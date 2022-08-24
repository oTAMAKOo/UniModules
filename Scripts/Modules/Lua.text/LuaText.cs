
using System.Collections.Generic;
using System.Text;
using Extensions;

namespace Modules.Lua.Text
{
    public sealed class LuaText
    {
        //----- params -----

        //----- field -----

		private AesCryptoKey aesCryptoKey = null;

		private Dictionary<string, string> texts = null;

        //----- property -----

        //----- method -----

		public LuaText(AesCryptoKey aesCryptoKey)
		{
			this.aesCryptoKey = aesCryptoKey;
		}

		public void Set(LuaTextAsset textAsset)
		{
			if (texts == null)
			{
				texts = new Dictionary<string, string>();
			}

			foreach (var content in textAsset.Contents)
			{
				foreach (var item in content.texts)
				{
					var text = item.Text.Decrypt(aesCryptoKey);	

					// エスケープされた改行コードを置き換え.
					text = text.Replace("\\n", "\n");

					texts[item.Id] = text;
				}
			}
		}

		public string Get(string id)
		{
			return texts.GetValueOrDefault(id);
		}
		
		public static string GetAssetFileName(string fileName, string identifier)
		{
			var fileNameBuilder = new StringBuilder();

			fileNameBuilder.Append(fileName);

			if (!string.IsNullOrEmpty(identifier))
			{
				fileNameBuilder.AppendFormat("-{0}", identifier);
			}

			fileNameBuilder.Append(".asset");
            
			return fileNameBuilder.ToString();
		}
    }
}