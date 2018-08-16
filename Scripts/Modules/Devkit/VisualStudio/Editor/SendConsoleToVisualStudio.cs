
#if ENABLE_VSTU && UNITY_EDITOR_WIN

using UnityEditor;
using UnityEngine;
using SyntaxTree.VisualStudio.Unity.Bridge.Configuration;

namespace VisualStudioToolsUnity
{
    public static class SendConsoleToVisualStudio
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            // VisualStudioにUnityのログを送らない.
            if (Configurations.Active.SendConsoleToVisualStudio)
            {
                Configurations.Active.SendConsoleToVisualStudio = false;
                Configurations.Active.Write();
            }
        }
    }
}

#endif
