
#if UNITY_EDITOR

using UnityEngine;
using System;

namespace Modules.PatternTexture
{
    public sealed partial class PatternImage
    {
        //----- params -----

        //----- field -----

        // 仮表示用のパターン (保存しない).
        [NonSerialized]
        private PatternTexture dummyPatternTexture = null;
        [NonSerialized]
        private string dummyPatternName = null;

        //----- property -----

        /// <summary> 仮表示のパターンを描画しているか (エディタの非実行時で PatternTexture が未設定の場合のみ) </summary>
        public bool IsDummyActive
        {
            get
            {
                if (Application.isPlaying) { return false; }

                if (patternTexture != null) { return false; }

                if (dummyPatternTexture == null) { return false; }

                return DummySource != null;
            }
        }

        /// <summary> 仮表示のパターン情報 </summary>
        public PatternData DummySource
        {
            get
            {
                if (dummyPatternTexture == null) { return null; }

                return dummyPatternTexture.GetPatternData(dummyPatternName);
            }
        }

        //----- method -----

        /// <summary> 仮表示のパターンを設定 </summary>
        internal void SetDummy(PatternTexture texture, string patternName)
        {
            if (dummyPatternTexture == texture && dummyPatternName == patternName) { return; }

            dummyPatternTexture = texture;
            dummyPatternName = patternName;

            SetAllDirty();
        }

        /// <summary> 仮表示のパターンを解除 </summary>
        internal void ClearDummy()
        {
            if (dummyPatternTexture == null && dummyPatternName == null) { return; }

            dummyPatternTexture = null;
            dummyPatternName = null;

            SetAllDirty();
        }
    }
}

#endif
