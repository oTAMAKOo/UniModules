
using UnityEngine;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.ExternalResource
{
    public sealed class ExternalResourceS3Uploader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async UniTask<bool> Upload(S3Uploader uploader)
        {
	        var exportPath = BuildManager.GetExportPath();

	        if (string.IsNullOrEmpty(exportPath)) { return false; }

	        var platformName = PlatformUtility.GetPlatformTypeName();

	        var sw = System.Diagnostics.Stopwatch.StartNew();

	        var result = await uploader.Execute(exportPath, platformName);

	        sw.Stop();

	        var success = !string.IsNullOrEmpty(result);

	        using (new DisableStackTraceScope())
	        {
		        if (success)
		        {
			        Debug.LogFormat("Upload Complete. ({0:F2}sec)\n\nVersion : {1}", sw.Elapsed.TotalSeconds, result);
		        }
		        else
		        {
			        Debug.LogError("Upload Failed.");
		        }
	        }

	        return success;
        }
    }
}