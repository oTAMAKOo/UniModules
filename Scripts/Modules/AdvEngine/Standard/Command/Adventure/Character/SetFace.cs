
using System;
using UniRx;
using Modules.AdvKit;
using MoonSharp.Interpreter;

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
            return (Func<string, int, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, int face, bool wait = false)
        {
            var advEngine = AdvEngine.Instance;

            var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

            var dicingImage = advCharacter.DicingImage;

            dicingImage.PatternName = face.ToString();

            if (dicingImage.CrossFade)
            {
                var timeSpan = TimeSpan.FromSeconds(dicingImage.CrossFadeTime + 1);

                Action onFaceSwitchComplete = () =>
                {
                    if(wait)
                    {
                        advEngine.Resume();
                    }
                };

                Observable.Timer(timeSpan)
                    .Subscribe(_ => onFaceSwitchComplete())
                    .AddTo(Disposable);
            }

            return wait && dicingImage.CrossFade ? YieldWait : DynValue.Nil;
        }
    }
}