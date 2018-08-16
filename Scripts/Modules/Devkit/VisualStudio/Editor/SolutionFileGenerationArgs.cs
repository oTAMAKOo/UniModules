#if ENABLE_VSTU

namespace VisualStudioToolsUnity
{
    /// <summary>
    /// ソリューション ファイル (.sln) 生成のフック処理にデータを提供.
    /// </summary>
    public class SolutionFileGenerationArgs : FileGenerationArgs
    {
        /// <summary>
        /// ソリューション ファイル (.sln) 本体を文字列形式で取得または設定します。
        /// </summary>
        public string Content { get; set; }

        internal SolutionFileGenerationArgs(string filename, string content) : base(filename)
        {
            this.Content = content;
        }
    }
}

#endif
