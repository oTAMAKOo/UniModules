
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    [ExecuteAlways]
    public abstract class TextEffectBase : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private bool alive = true;

        private Text target = null;

        //----- property -----

        public bool Alive { get { return alive; } }

        public Text Text
        {
            get { return target ?? (target = UnityUtility.GetComponent<Text>(gameObject)); }
        }

        //----- method -----
                
        void OnEnable()
        {
            TextEffectManager.Instance.Apply(this);
        }

        void OnDestroy()
        {
            alive = false;

            TextEffectManager.Instance.Apply(this);
        }

        public abstract void SetShaderParams(Material material);

        public abstract string GetCacheKey();
    }
}
