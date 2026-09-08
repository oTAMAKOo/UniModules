
namespace Modules.Automation.PrefabPreview
{
    /// <summary> Prefabプレビュー描画の結果 </summary>
    public sealed class PrefabPreviewResult
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public string PrefabAssetPath { get; private set; }

        public string OutputFilePath { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        //----- method -----

        public PrefabPreviewResult(string prefabAssetPath, string outputFilePath, int width, int height)
        {
            PrefabAssetPath = prefabAssetPath;
            OutputFilePath = outputFilePath;
            Width = width;
            Height = height;
        }
    }
}
