
using UnityEngine.UI;
using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class FadeIn : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "FadeIn"; } }

        public Image FadeImage { get; set; }

        //----- method -----

        public override void Initialize()
        {
            UnityUtility.SetActive(FadeImage, false);
        }

        public override object GetCommandDelegate()
        {
            return (Func<float, string, string, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(float time = 0.4f, string colorCode = "#000000", string easingType = null)
        {
            var advEngine = AdvEngine.Instance;

            var color = colorCode.HexToColor();

            color.a = 1f;

            UnityUtility.SetActive(FadeImage, true);

            TweenCallback onComplete = () =>
            {
                UnityUtility.SetActive(FadeImage, false);
                advEngine.Resume();
            };

            FadeImage.color = color;

            var ease = EnumExtensions.FindByName(easingType, Ease.Linear);

            var tweener = FadeImage.DOFade(0f, time)
                .SetEase(ease)
                .OnComplete(onComplete);

            advEngine.SetTweenTimeScale(tweener);

            return YieldWait;
        }
    }
}