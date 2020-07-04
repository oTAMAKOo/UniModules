
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public sealed class StateController<T> : LifetimeDisposable where T : Enum
    {
        //----- params -----

        private sealed class StateEmptyArgument : StateArgument { }

        //----- field -----

        private State<T> currentState = null;

        private Dictionary<T, State<T>> stateTable = null;

        private IDisposable changeStateDisposable = null;

        private bool isExecute = false;

        //----- property -----

        public T Current { get { return currentState != null ? currentState.Type : default; } }

        //----- method -----

        public StateController()
        {
            stateTable = new Dictionary<T, State<T>>();
        }

        /// <summary> ノードを取得 </summary>
        public State<T> Get(T state)
        {
            var stateInstance = stateTable.GetValueOrDefault(state);

            if (stateInstance == null)
            {
                stateInstance = new State<T>(state);
                
                stateTable[state] = stateInstance;
            }

            return stateInstance;
        }

        /// <summary> 登録されたステートをクリア </summary>
        public void Clear()
        {
            if (changeStateDisposable != null)
            {
                changeStateDisposable.Dispose();
                changeStateDisposable = null;
            }

            stateTable.Clear();

            currentState = null;
        }

        /// <summary> ステート変更を要求 </summary>
        public void Request(T next, bool force = false)
        {
            Request(next, new StateEmptyArgument(), force);
        }

        /// <summary> ステート変更を要求 </summary>
        public void Request<TArgument>(T next, TArgument argument, bool force = false) where TArgument : StateArgument, new()
        {
            if (isExecute)
            {
                if (force)
                {
                    if (changeStateDisposable != null)
                    {
                        changeStateDisposable.Dispose();
                        changeStateDisposable = null;
                    }
                }
                else
                {
                    return;
                }
            }

            isExecute = true;

            // ※ changeStateDisposableで実行中判定しようとすると中でyield breakしているだけの場合Finallyが先に呼ばれてしまう為実行中フラグで管理する.

            changeStateDisposable = Observable.FromCoroutine(() => ChangeState(next, argument))
                .Finally(() =>
                    {
                        changeStateDisposable = null;
                        isExecute = false;
                    })
                .Subscribe()
                .AddTo(Disposable);
        }

        private IEnumerator ChangeState<TArgument>(T next, TArgument argument) where TArgument : StateArgument
        {
            var prevState = currentState;

            var nextState = stateTable.GetValueOrDefault(next);

            if (nextState == null)
            {
                throw new KeyNotFoundException(string.Format("This state is not registered. Type: {0}", next));
            }

            // 前のステートの終了待ち.

            if (prevState != null)
            {
                var exitYield = Exit(prevState, nextState.Type).ToYieldInstruction();

                while (!exitYield.IsDone)
                {
                    yield return null;
                }
            }

            // 現在のステートを更新.

            currentState = nextState;

            // ステートの開始.

            var enterYield = Enter(currentState, argument).ToYieldInstruction();

            while (!enterYield.IsDone)
            {
                yield return null;
            }
        }

        /// <summary> 開始処理実行 </summary>
        private IEnumerator Enter<TArgument>(State<T> state, TArgument argument) where TArgument : StateArgument
        {
            var enterFunctions = state.GetEnterFunctions();

            foreach (var functions in enterFunctions)
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
        private IEnumerator Exit(State<T> state, T nextState)
        {
            var exitFunctions = state.GetExitFunctions();

            foreach (var functions in exitFunctions)
            {
                var observers = new List<IObservable<Unit>>();

                foreach (var function in functions)
                {
                    var func = function;

                    var observer = Observable.Defer(() => Observable.FromCoroutine(() => func(nextState)));

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
