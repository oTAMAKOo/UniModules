
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class Move : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "Move"; } }

        //----- method -----

        public override object GetCommandDelegate()
        {
            return (Func<string, Vector3, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, Vector3 position, float duration = 0, string easingType = null, bool wait = true)
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

                    var tweener = advObject.transform.DOLocalMove(position, duration)
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
