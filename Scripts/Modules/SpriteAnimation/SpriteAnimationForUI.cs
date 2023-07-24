
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.SpriteAnimation
{
    [RequireComponent(typeof(Image))]
    public sealed class SpriteAnimationForUI : SpriteAnimation
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private bool playOnEnable = false;
        [SerializeField]
        private string animationName = null;
        [SerializeField]
        private int startIndex = 0;
        [SerializeField]
        private bool loop = true;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var targetImage = UnityUtility.GetComponent<Image>(gameObject);

            if (targetImage != null)
            {
                OnUpdateAnimationAsObservable()
                    .TakeUntilDisable(this)
                    .Subscribe(x => targetImage.sprite = x)
                    .AddTo(this);

                if (playOnEnable && !string.IsNullOrEmpty(animationName))
                {
                    Play(animationName, startIndex, loop);
                }
            }
        }

        public void SetAnimationName(string animationName)
        {
            this.animationName = animationName;
        }

        public void SetLoop(bool loop)
        {
            this.loop = loop;
        }
    }
}