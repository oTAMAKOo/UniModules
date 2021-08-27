
#if ENABLE_MOONSHARP

using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class ScaleX : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "ScaleX"; } }

        //----- method -----

        public override object GetCommandDelegate()
        {
            return (Func<string, float, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, float scale, float duration = 0, string easingType = null, bool wait = true)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

                if (advObject != null)
                {
                    TweenCallback onComplete = () =>
                    {
                        if (wait)
                        {
                            advEngine.Resume();
                        }
                    };

                    var ease = EnumExtensions.FindByName(easingType, Ease.Linear);

                    var tweener = advObject.transform.DOScaleX(scale, duration)
                        .SetEase(ease)
                        .OnComplete(onComplete);

                    advEngine.SetTweenTimeScale(tweener);
                }

                returnValue = wait ? YieldWait : DynValue.Nil;
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
