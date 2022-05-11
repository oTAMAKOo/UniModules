
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Animation;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class PlayAnimation : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlayAnimation"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string animation, bool wait = true)
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

                if (advObject != null)
                {
                    var animationPlayer = UnityUtility.GetComponent<AnimationPlayer>(advObject);

                    if (animationPlayer != null)
                    {
                        Action onComplete = () =>
                        {
                            if (wait)
                            {
                                advEngine.Resume();
                            }
                        };

                        animationPlayer.Play(animation)
							.ToObservable()
                            .Subscribe(_ => onComplete())
                            .AddTo(Disposable);
                    }
                    else
                    {
                        Debug.LogErrorFormat("AnimationPlayer component not found. [{0}]", identifier);
                    }
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
