
#if ENABLE_MOONSHARP

using System;
using UniRx;
using Modules.AdvKit;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class SetFace : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetFace"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string patternName, bool wait = false)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

                var patternImage = advCharacter.PatternImage;

                patternImage.PatternName = patternName;

                if (patternImage.CrossFade)
                {
                    var timeSpan = TimeSpan.FromSeconds(patternImage.CrossFadeTime + 1);

                    Action onFaceSwitchComplete = () =>
                    {
                        if (wait)
                        {
                            advEngine.Resume();
                        }
                    };

                    Observable.Timer(timeSpan)
                        .Subscribe(_ => onFaceSwitchComplete())
                        .AddTo(Disposable);
                }

                returnValue = wait && patternImage.CrossFade ? YieldWait : DynValue.Nil;
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
