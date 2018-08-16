
#if ENABLE_VSTU

using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualStudioToolsUnity
{
    /// <summary>
    /// VSTU のファイル生成をフックする機能を提供.
    /// </summary>
    /// <typeparam name="T">ハンドラーに提供する引数の型。</typeparam>
    public abstract class FileGeneration<T> where T : FileGenerationArgs
    {
        private static readonly Func<string, bool> trueSelector = _ => true;
        private readonly List<SelectorHandlerPair<T>> handlers = new List<SelectorHandlerPair<T>>();

        /// <summary>
        /// <see cref="AddHook(Func&lt;string, bool&gt;, Action&lt;T&gt;)"/> によって追加されたハンドラーを列挙します。
        /// </summary>
        protected IEnumerable<SelectorHandlerPair<T>> Handlers
        {
            get { return handlers; }
        }

        /// <summary>
        /// ファイル作成処理をフックするハンドラーを追加.
        /// </summary>
        /// <param name="handler">追加するハンドラー。</param>
        public void AddHook(Action<T> handler)
        {
            this.AddHook(trueSelector, handler);
        }

        /// <summary>
        /// フックする対象を指定するセレクターを指定して、ファイル作成処理をフックするハンドラーを追加.
        /// </summary>
        /// <param name="nameSelector">フックする対象のファイルかどうかをテストする関数。</param>
        /// <param name="handler">追加するハンドラー。</param>
        public void AddHook(Func<string, bool> nameSelector, Action<T> handler)
        {
            var pair = new SelectorHandlerPair<T> { NameSelector = nameSelector, Handler = handler };
            handlers.Add(pair);
        }

        /// <summary>
        /// <see cref="AddHook(Action&lt;T&gt;)"/> によって追加されたハンドラーを削除.
        /// </summary>
        /// <param name="handler">削除するハンドラー。</param>
        public void RemoveHook(Action<T> handler)
        {
            this.RemoveHook(trueSelector, handler);
        }

        /// <summary>
        /// <see cref="AddHook(Func&lt;string, bool&gt;, Action&lt;T&gt;)"/> によって追加されたハンドラーを削除.
        /// </summary>
        /// <param name="nameSelector">フックする対象のファイルかどうかをテストする関数。</param>
        /// <param name="handler">削除するハンドラー。</param>
        public void RemoveHook(Func<string, bool> nameSelector, Action<T> handler)
        {
            var target = handlers.FirstOrDefault(x => x.NameSelector == nameSelector && x.Handler == handler);

            if (target != null)
            {
                handlers.Remove(target);
            }
        }

        /// <summary>
        /// フック条件をテストする関数とハンドラーのペアを表します.
        /// </summary>
        /// <typeparam name="THandler">ハンドラーに提供する引数の型。</typeparam>
        protected class SelectorHandlerPair<THandler>
        {
            /// <summary>
            /// フック条件をテストする関数を取得または設定します。
            /// </summary>
            public Func<string, bool> NameSelector { get; set; }

            /// <summary>
            /// フック処理を実行するハンドラーを取得または設定します。
            /// </summary>
            public Action<THandler> Handler { get; set; }
        }
    }
}

#endif
