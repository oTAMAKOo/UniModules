﻿
using Extensions;
using System.IO;
using System.Text;

namespace Modules.Devkit.AssetTuning
{
    public sealed class ScriptAssetTuner : AssetTuner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override int Priority { get { return 100; } }

        //----- method -----

        public override bool Validate(string path)
        {
            var extension = Path.GetExtension(path);
            
            return extension == ".cs";
        }

		public override void OnAssetImport(string assetPath)
		{
			var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

			if(!File.Exists(path)){ return; }

            byte[] bytes = null;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[fs.Length];

                fs.Read(bytes, 0, bytes.Length);
            }
            
            var encode = Encode.GetEncode(bytes);

            if(encode != null)
            {
				var str = encode.GetString(bytes);

				// UTF8 + BOM付きか確認.
				var isUTF8Encoding = encode.CodePage == 65001 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

                // 改行コードの置き換え.
                var requireLineEndReplace = str.Contains("\r\n");

				if (requireLineEndReplace)
				{
					// 改行コードの置き換え.
					str = str.Replace("\r\n", "\n");
				}

                // 保存.
				if (!isUTF8Encoding || requireLineEndReplace)
				{
					File.WriteAllText(path, str, new UTF8Encoding(true));
				}
            }
        }
    }
}