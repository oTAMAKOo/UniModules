
using UnityEngine;
using System;

namespace Modules.Automation.PrefabPreview
{
    /// <summary> Prefabプレビュー描画のオプション </summary>
    public sealed class PrefabPreviewOptions
    {
        //----- params -----

        public const int DefaultWidth = 1920;

        public const int DefaultHeight = 1080;

        //----- field -----

        //----- property -----

        /// <summary> 描画解像度の幅(px). キャンバスの幅も同値になる </summary>
        public int Width { get; set; }

        /// <summary> 描画解像度の高さ(px). キャンバスの高さも同値になる </summary>
        public int Height { get; set; }

        /// <summary> 背景色 </summary>
        public Color BackgroundColor { get; set; }

        /// <summary> インスタンス生成直後(レイアウト再計算前)に一度呼ばれるフック </summary>
        public Action<GameObject> OnInstantiated { get; set; }

        //----- method -----

        public PrefabPreviewOptions()
        {
            Width = DefaultWidth;
            Height = DefaultHeight;
            BackgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            OnInstantiated = null;
        }
    }
}
