
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public abstract class StateArgument { }

    public abstract class StateNodeBase<T> where T : Enum
    {
        protected bool initialized = false;

        public T State { get; private set; }

        public virtual void Initialize(T state)
        {
            if (initialized){ return; }

            State = state;

            initialized = true;
        }

        public abstract IEnumerator Enter();

        public abstract IEnumerator Exit();
    }

    public sealed class StateNode<T> : StateNode<T, StateNode<T>.EmptyArgument> where T : Enum
    {
        //----- params -----

        public sealed class EmptyArgument : StateArgument { }

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> 開始イベント追加 </summary>
        public void AddEnterFunction(Func<IEnumerator> function, int priority = 0)
        {
            var list = enterFunctions.GetOrAdd(priority, i => new List<Func<EmptyArgument, IEnumerator>>());

            Func<EmptyArgument, IEnumerator> enterFunction = x =>
            {
                return function.Invoke();
            };

            list.Add(enterFunction);
        }
    }

    public class StateNode<T, TArgument> : StateNodeBase<T> where T : Enum where TArgument : StateArgument, new()
    {
        //----- params -----

        //----- field -----

        protected SortedDictionary<int, List<Func<TArgument, IEnumerator>>> enterFunctions = null;

        protected SortedDictionary<int, List<Func<IEnumerator>>> exitFunctions = null;

        protected TArgument argument = null;

        //----- property -----

        //----- method -----

        public override void Initialize(T state)
        {
            if (initialized){ return; }

            base.Initialize(state);

            enterFunctions = new SortedDictionary<int, List<Func<TArgument, IEnumerator>>>();
            exitFunctions = new SortedDictionary<int, List<Func<IEnumerator>>>();
        }

        public void SetArgument(StateArgument enterArgument)
        {
            if (enterArgument is StateNode<T>.EmptyArgument)
            {
                argument = null;
            }
            else
            {
                argument = enterArgument as TArgument;
            }
        }

        /// <summary> 開始イベント追加 </summary>
        public void AddEnterFunction(Func<TArgument, IEnumerator> function, int priority = 0)
        {
            var list = enterFunctions.GetOrAdd(priority, i => new List<Func<TArgument, IEnumerator>>());

            list.Add(function);
        }

        /// <summary> 終了イベント追加 </summary>
        public void AddExitFunction(Func<IEnumerator> function, int priority = 0)
        {
            var list = exitFunctions.GetOrAdd(priority, i => new List<Func<IEnumerator>>());

            list.Add(function);
        }

        /// <summary> 開始処理実行 </summary>
        public override IEnumerator Enter()
        {
            foreach (var functions in enterFunctions.Values)
            {
                var observers = new List<IObservable<Unit>>();

                foreach (var function in functions)
                {
                    var func = function;

                    var observer = Observable.Defer(() => Observable.FromCoroutine(() => func(argument)));

                    observers.Add(observer);
                }

                var enterYield = observers.WhenAll().ToYieldInstruction();
                
                while (!enterYield.IsDone)
                {
                    yield return null;
                }
            }
        }

        /// <summary> 終了処理実行 </summary>
        public override IEnumerator Exit()
        {
            foreach (var functions in exitFunctions.Values)
            {
                var observers = new List<IObservable<Unit>>();

                foreach (var function in functions)
                {
                    var func = function;

                    var observer = Observable.Defer(() => Observable.FromCoroutine(() => func()));

                    observers.Add(observer);
                }

                var exitYield = observers.WhenAll().ToYieldInstruction();

                while (!exitYield.IsDone)
                {
                    yield return null;
                }
            }
        }
    }
}
