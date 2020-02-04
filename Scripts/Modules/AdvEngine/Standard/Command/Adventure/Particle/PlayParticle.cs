
#if ENABLE_MOONSHARP

using System;
using UniRx;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class PlayParticle : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlayParticle"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, bool, int?, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string fileIdentifier, bool wait = true, int? sortingOrder = null, bool restart = true)
        {
            var advEngine = AdvEngine.Instance;
            
            var advParticle = advEngine.ObjectManager.Create<AdvParticle>(identifier);

            var fileName = advEngine.Resource.FindFileName<AdvParticle>(fileIdentifier);

            if (advParticle != null)
            {
                Action onComplete = () =>
                {
                    if (wait)
                    {
                        advEngine.Resume();
                    }
                };

                advParticle.Play(fileName, restart, sortingOrder)
                    .Subscribe(_ => onComplete())
                    .AddTo(Disposable);
            }

            return wait ? YieldWait : DynValue.Nil;
        }
    }
}

#endif
