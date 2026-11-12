
using UnityEngine;
using UnityEngine.UIElements;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Markdown
{
    /// <summary>
    /// Markdownの整形表示の標準設定.
    /// 標準のスタイルシートと、常に有効なルールを持つ.
    /// </summary>
    public sealed class DefaultMarkdownViewConfig : SingletonScriptableObject<DefaultMarkdownViewConfig>
    {
        //----- params -----

        //----- field -----

        [Tooltip("Style sheet applied to rules that have no style sheet.")]
        [SerializeField]
        private StyleSheet defaultStyleSheet = null;

        [SerializeField]
        private MarkdownViewRule[] rules = null;

        //----- property -----

        /// <summary> スタイルシートが未設定のルールに適用するスタイルシート </summary>
        public StyleSheet DefaultStyleSheet { get { return defaultStyleSheet; } }

        /// <summary> 標準のルール. MarkdownViewConfig のルールの後に判定される. </summary>
        public MarkdownViewRule[] Rules
        {
            get { return rules ?? (rules = new MarkdownViewRule[0]); }
        }

        //----- method -----
    }
}
