
using UnityEngine;
using Extensions;

namespace Modules.PatternTexture
{
    /// <summary> エディタの非実行時だけ PatternImage に仮のパターンを表示する (参照するアセットとパターン名は保存しない) </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatternImage))]
    public sealed partial class DummyPatternImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private PatternImage patternImage = null;

        //----- property -----

        public PatternImage PatternImage
        {
            get
            {
                return patternImage ?? (patternImage = UnityUtility.GetComponent<PatternImage>(gameObject));
            }
        }

        //----- method -----
    }
}
