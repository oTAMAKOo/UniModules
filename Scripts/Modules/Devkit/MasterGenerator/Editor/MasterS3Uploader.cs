
using UnityEngine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Modules.Master.Editor;

namespace Modules.Master
{
    public sealed class MasterS3Uploader 
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async Task<bool> Upload(S3Uploader uploader)
        {
	        var exportPath = MasterGenerator.GetExportDirectory();

	        if (string.IsNullOrEmpty(exportPath))
	        {
		        Debug.LogError("Export path is empty.");

		        return false;
	        }

			// バージョンファイルからルートハッシュを取得.

	        var versionFilePath = PathUtility.Combine(exportPath, MasterGenerator.VersionFileName);

	        if (!File.Exists(versionFilePath))
	        {
		        Debug.LogErrorFormat("VersionFile not found.\n{0}", versionFilePath);

		        return false;
	        }

	        var rootHash = string.Empty;

	        using (var streamReader = new StreamReader(versionFilePath, Encoding.UTF8, false))
	        {
				rootHash = await streamReader.ReadLineAsync();
	        }

	        if (string.IsNullOrEmpty(rootHash))
	        {
		        Debug.LogError("RootHash empty.");

		        return false;
	        }

			// アップロード.

	        var sw = System.Diagnostics.Stopwatch.StartNew();

	        var result = await uploader.Execute(exportPath, rootHash);

	        sw.Stop();

	        using (new DisableStackTraceScope())
	        {
		        if (result)
		        {
			        Debug.LogFormat("Upload Complete. ({0:F2}sec)\n\nVersion: {1}", sw.Elapsed.TotalSeconds, rootHash);
		        }
		        else
		        {
			        Debug.LogError("Upload Failed.");
		        }
	        }

	        return result;
        }
	}
}