
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public abstract class StateArgument { }

    public interface IState<T> where T : Enum
    {
        T Type { get; }

        IEnumerator Enter();

        IEnumerator Exit();
    }

    public abstract class State<T> : IState<T> where T : Enum
    {
        //----- params -----

        //----- field -----

        private SortedDictionary<int, List<Func<IEnumerator>>> enterFunctions = null;

        private SortedDictionary<int, List<Func<IEnumerator>>> exitFunctions = null;

        //----- property -----

        public abstract T Type { get; }

        //----- method -----

        public State()
        {
            enterFunctions = new SortedDictionary<int, List<Func<IEnumerator>>>();
            exitFunctions = new SortedDictionary<int, List<Func<IEnumerator>>>();
        }

        /// <summary> 開始イベント追加 </summary>
        public void AddEnterFunction(Func<IEnumerator> function, int priority = 0)
        {
            var list = enterFunctions.GetOrAdd(priority, i => new List<Func<IEnumerator>>());

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
                var enterYield = item.Select(x => Observable.FromCoroutine(x)).WhenAll().ToYieldInstruction();

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

    public abstract class State<T, TArgument> : IState<T> where T : Enum where TArgument : StateArgument, new()
    {
        //----- params -----

        //----- field -----

        private SortedDictionary<int, List<Func<TArgument, IEnumerator>>> enterFunctions = null;

        private SortedDictionary<int, List<Func<IEnumerator>>> exitFunctions = null;

        private TArgument argument = null;

        //----- property -----

        public abstract T Type { get; }

        //----- method -----

        public State()
        {
            enterFunctions = new SortedDictionary<int, List<Func<TArgument, IEnumerator>>>();
            exitFunctions = new SortedDictionary<int, List<Func<IEnumerator>>>();
        }

        public void SetArgument(StateArgument enterArgument)
        {
            argument = enterArgument as TArgument;

            if (argument == null)
            {
                argument = new TArgument();
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
