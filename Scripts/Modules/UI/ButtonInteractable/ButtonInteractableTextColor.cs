
using UnityEngine;
using UniRx;
using Extensions;
using Modules.UI.Extension;

namespace Modules.UI.Reactive
{
    [ExecuteAlways]
    [RequireComponent(typeof(UIText))]
    public sealed class ButtonInteractableTextColor : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UIButton target = null;
        [SerializeField]
        private Color enableColor = Color.white;
        [SerializeField]
        private Color disableColor = Color.black;

        private UIText uiText = null;

        //----- property -----

        public Color EnableColor
        {
            get { return enableColor; }
            set { enableColor = value; }
        }

        public Color DisableColor
        {
            get { return disableColor; }
            set { disableColor = value; }
        }

        //----- method -----

        void Awake()
        {
            uiText = UnityUtility.GetComponent<UIText>(gameObject);

            if (target != null && Application.isPlaying)
            {
                target.ObserveEveryValueChanged(x => x.Button.interactable).Subscribe(x => Apply(x)).AddTo(this);
            }
        }

        void OnEnable()
        {
            if (target != null)
            {
                Apply(target.Button.interactable);
            }
        }

        private void Apply(bool interactable)
        {
            var color = interactable ? enableColor : disableColor;

            uiText.Text.color = color;
        }
    }
}