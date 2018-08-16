
#if ENABLE_VSTU

using System;
using System.Xml.Linq;

namespace VisualStudioToolsUnity
{
    /// <summary>
    /// プロジェクト ファイル (.csproj) 生成のフック処理にデータを提供.
    /// </summary>
    public class ProjectFileGenerationArgs : FileGenerationArgs
    {
        private string rawContent;

        /// <summary>
        /// プロジェクト ファイル (.csproj) 本体を XML 形式で取得.
        /// </summary>
        public XDocument Content { get; private set; }

        /// <summary>
        /// プロジェクト ファイル (.csproj) 本体を文字列形式で取得または設定.
        /// </summary>
        public string RawContent
        {
            get { return rawContent; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");

                rawContent = value;
                Content = XDocument.Parse(value);
            }
        }

        internal ProjectFileGenerationArgs(string filename, string content) : base(filename)
        {
            RawContent = content;
        }
    }
}

#endif
