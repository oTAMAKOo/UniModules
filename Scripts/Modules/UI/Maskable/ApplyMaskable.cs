
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;

namespace Modules.UI.Maskable
{
    public sealed class ApplyMaskable : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private MaskableGraphic[] ignoreMaskables = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            SetMaskable();
        }

        public void SetMaskable()
        {
            var targetGraphic = gameObject.Descendants()
                .OfComponent<MaskableGraphic>()
                .Where(x => !ignoreMaskables.Contains(x));

            foreach (var target in targetGraphic)
            {
                if (target.maskable){ continue; }

                target.maskable = true;
            }
        }
    }
}
