
using UnityEngine;
using System;
using System.Collections;
using UniRx;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class Wait : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "Wait"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<float, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(float seconds)
        {
            var advEngine = AdvEngine.Instance;

            Observable.FromMicroCoroutine(() => WaitTimer(seconds))
                .Subscribe(_ => advEngine.Resume())
                .AddTo(Disposable);

            return YieldWait;
        }

        private IEnumerator WaitTimer(float seconds)
        {
            var advEngine = AdvEngine.Instance;

            var time = 0f;

            while (time < seconds)
            {
                time += Time.deltaTime * advEngine.TimeScale;

                yield return null;
            }
        }
    }
}