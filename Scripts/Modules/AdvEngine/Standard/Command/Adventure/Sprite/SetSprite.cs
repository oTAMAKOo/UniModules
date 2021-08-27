
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class SetSprite : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetSprite"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, Vector2?, int?, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string fileIdentifier, Vector2? size, int? priority = null,
                                         float duration = 0, string easingType = null, bool wait = true)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advSprite = advEngine.ObjectManager.Create<AdvSprite>(identifier);

                var fileName = advEngine.Resource.FindFileName<AdvSprite>(fileIdentifier);

                if (advSprite != null)
                {
                    advSprite.SetPriority(priority.HasValue ? priority.Value : 0);

                    var width = size.HasValue ? (float?)size.Value.x : null;
                    var height = size.HasValue ? (float?)size.Value.y : null;

                    advSprite.Show(fileName, width, height);
                }

                var canvasGroup = UnityUtility.GetComponent<CanvasGroup>(advSprite);

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
