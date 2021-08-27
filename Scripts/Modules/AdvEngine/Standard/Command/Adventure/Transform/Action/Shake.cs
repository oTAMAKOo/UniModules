
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using DG.Tweening;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class Shake : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "Shake"; } }

        //----- method -----        
        
        public override object GetCommandDelegate()
        {
            return (Func<string, float, Vector2, int, float, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, float duration, Vector2 strength, int vibrato = 10, float randomness = 90f, bool wait = true)
        {
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

                    var tweener = advObject.transform
                        .DOShakePosition(duration, strength, vibrato, randomness)
                        .OnComplete(onComplete);

                    advEngine.SetTweenTimeScale(tweener);

                    return wait ? YieldWait : DynValue.Nil;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return DynValue.Nil;
        }
    }
}

#endif
