
using UnityEditor;
using System.Diagnostics;

namespace Modules.Devkit
{
    #if UNITY_EDITOR_OSX

    public static class LaunchUnityForMac
    {
	    [MenuItem("Help/Launch Unity", priority = 150)]
	    private static void StartNewUnity()
        {
            const string MacUnityPath = "MacOS/Unity";

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = EditorApplication.applicationContentsPath + "/" + MacUnityPath,
                UseShellExecute = true,
                CreateNoWindow = true,
            };

		    Process.Start(processStartInfo);
	    }
    }

    #endif
}