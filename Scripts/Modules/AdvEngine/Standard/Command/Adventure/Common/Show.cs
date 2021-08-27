
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class Show : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "Show"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, float duration = 0, string easingType = null, bool wait = true)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

                if (advObject == null) { return DynValue.Nil; }

                UnityUtility.SetActive(advObject, true);

                var canvasGroup = UnityUtility.GetComponent<CanvasGroup>(advObject);

                if (canvasGroup != null)
                {
                    if (duration != 0)
                    {
                        TweenCallback onComplete = () =>
                        {
                            if (wait)
                            {
                                advEngine.Resume();
                            }
                        };

                        var ease = EnumExtensions.FindByName(easingType, Ease.Linear);

                        var tweener = canvasGroup.DOFade(1f, duration)
                            .SetEase(ease)
                            .OnComplete(onComplete);

                        advEngine.SetTweenTimeScale(tweener);

                        returnValue = wait ? YieldWait : DynValue.Nil;
                    }
                    else
                    {
                        canvasGroup.alpha = 1f;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return returnValue;
        }
    }
}

#endif
