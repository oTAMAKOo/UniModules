
using UnityEngine;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Markdown
{
    /// <summary>
    /// Markdownの整形表示の設定.
    /// 対象にするファイル名と、適用するスタイルシートの組を追加する.
    /// このアセットが無い場合は、組み込みのルールだけで動作する.
    /// </summary>
    public sealed class MarkdownViewConfig : SingletonScriptableObject<MarkdownViewConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private MarkdownViewRule[] rules = null;

        //----- property -----

        /// <summary> 追加のルール. 並び順が優先順になり、組み込みのルールより先に判定される. </summary>
        public MarkdownViewRule[] Rules
        {
            get { return rules ?? (rules = new MarkdownViewRule[0]); }
        }

        //----- method -----

        /// <summary> 設定アセットを取得する. アセットが無い場合は null を返す. </summary>
        public static MarkdownViewConfig FindInstance()
        {
            if (!UnityUtility.IsNull(instance)){ return instance; }

            var asset = UnityEditorUtility.FindAssetsByType<MarkdownViewConfig>($"t:{typeof(MarkdownViewConfig).FullName}").FirstOrDefault();

            if (asset == null){ return null; }

            return Instance;
        }
    }
}
