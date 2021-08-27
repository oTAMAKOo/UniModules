
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class SetCharacter : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetCharacter"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, Vector2, int?, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string patternName, Vector2 pos, int? priority = null,
                                     float duration = 0, string easingType = null, bool wait = true)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

                if (advCharacter != null)
                {
                    advCharacter.SetPriority(priority.HasValue ? priority.Value : 0);

                    advCharacter.Show(patternName);

                    advCharacter.transform.localPosition = pos;
                }

                var canvasGroup = UnityUtility.GetComponent<CanvasGroup>(advCharacter);

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

                        canvasGroup.alpha = 0f;

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
