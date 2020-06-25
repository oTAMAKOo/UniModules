
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public abstract class StateArgument { }

    public interface IStateNode<T> where T : Enum
    {
        T State { get; }

        IEnumerator Enter();

        IEnumerator Exit();
    }

    public sealed class StateNode<T> : StateNode<T, StateNode<T>.EmptyArgument> where T : Enum
    {
        //----- params -----

        public sealed class EmptyArgument : StateArgument { }

        //----- field -----

        //----- property -----

        //----- method -----

        public StateNode(T state) : base(state) { }

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

    public class StateNode<T, TArgument> : IStateNode<T> where T : Enum where TArgument : StateArgument, new()
    {
        //----- params -----

        //----- field -----

        protected SortedDictionary<int, List<Func<TArgument, IEnumerator>>> enterFunctions = null;

        protected SortedDictionary<int, List<Func<IEnumerator>>> exitFunctions = null;

        protected TArgument argument = null;

        //----- property -----

        public T State { get; private set; }

        //----- method -----

        public StateNode(T state)
        {
            State = state;

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
        public IEnumerator Enter()
        {
            foreach (var item in enterFunctions.Values)
            {
                var enterYield = item.Select(x => Observable.FromCoroutine(() => x(argument))).WhenAll().ToYieldInstruction();

                while (!enterYield.IsDone)
                {
                    yield return null;
                }
            }
        }

        /// <summary> 終了処理実行 </summary>
        public IEnumerator Exit()
        {
            foreach (var item in exitFunctions.Values)
            {
                var exitYield = item.Select(x => Observable.FromCoroutine(x)).WhenAll().ToYieldInstruction();

                while (!exitYield.IsDone)
                {
                    yield return null;
                }
            }
        }
    }
}
