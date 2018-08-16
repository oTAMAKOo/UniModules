
#if ENABLE_VSTU

namespace VisualStudioToolsUnity
{
    /// <summary>
    /// ファイル生成のフック処理にデータを提供.
    /// </summary>
    public abstract class FileGenerationArgs
    {
        /// <summary>
        /// フックを処理済みとしてマークする値を取得または設定します。
        /// <see cref="Handled"/> の値を true に設定すると、以降のハンドラーをすべて無効化しフック処理を終了.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// フックを中止するかどうかを示す値を取得または設定.
        /// <see cref="Cancel"/> の値を true に設定すると、すべてのフック処理を中止しVSTUが生成したファイルをそのまま適用できます.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 生成されたファイル名を取得.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// 生成されたファイル名を指定して、<see cref="FileGenerationArgs"/> クラスの新しいインスタンスを初期化.
        /// </summary>
        /// <param name="filename">生成されたファイル名。</param>
        protected internal FileGenerationArgs(string filename)
        {
            this.Filename = filename;
            this.Handled = false;
            this.Cancel = false;
        }
    }
}

#endif
