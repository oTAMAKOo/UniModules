
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

        public override void OnAssetCreate(string path)
        {
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
                // "utf-8"以外を処理.
                if(encode.CodePage == 65001)
                {
                    // BOMを確認.
                    if(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) { return; }
                }

                // 改行コードの置き換え.
                var contents = encode.GetString(bytes).Replace("\r\n", "\n");

                // 保存.
                File.WriteAllText(path, contents, new UTF8Encoding(true));
            }
        }
    }
}