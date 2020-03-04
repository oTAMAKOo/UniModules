
#if ENABLE_VSTU && UNITY_2018_2_OR_NEWER

using UnityEditor;
using System;

namespace VisualStudioToolsUnity
{
    public sealed class VisualStudioPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 250;
        }

        /// <summary>
        /// Unity標準のジェネレータ「以外」でC#プロジェクトを生成するかどうか返すコールバック.
        /// </summary>
        static bool OnPreGeneratingCSProjectFiles()
        {
            return false;
        }

        /// <summary>
        /// ソリューションファイルが生成された後で修正を適用するコールバック.
        /// </summary>
        static string OnGeneratedSlnSolution(string path, string content)
        {
            return VisualStudioFileCallback.OnGeneratedSlnSolution(path, content);
        }

        /// <summary>
        /// プロジェクトファイルが生成された後で修正を適用するコールバック.
        /// </summary>
        static string OnGeneratedCSProject(string path, string content)
        {
            return VisualStudioFileCallback.OnGeneratedCSProject(path, content);
        }
    }
}

#endif
