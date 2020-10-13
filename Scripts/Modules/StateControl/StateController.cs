
using UnityEngine;
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

        private Subject<T> onChangeStateStart = null;

        private Subject<T> onChangeStateFinish = null;

        //----- property -----

        public T Current { get { return currentState != null ? currentState.Type : default; } }

        public bool IsExecute { get; private set; }

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
            if (IsExecute)
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

            IsExecute = true;

            // ※ changeStateDisposableで実行中判定しようとすると中でyield breakしているだけの場合Subscribeが先に呼ばれてしまう為実行中フラグで管理する.

            changeStateDisposable = Observable.FromCoroutine(() => ChangeState(next, argument))
                .ObserveOnMainThread()
                .Subscribe(_ =>
                   {
                       changeStateDisposable = null;
                       IsExecute = false;
                   })
                .AddTo(Disposable);
        }
        
        private IEnumerator ChangeState<TArgument>(T next, TArgument argument) where TArgument : StateArgument
        {
            var prev = Current;

            var prevState = currentState;

            var nextState = stateTable.GetValueOrDefault(next);

            if (nextState == null)
            {
                throw new KeyNotFoundException(string.Format("This state is not registered. Type: {0}", next));
            }

            if (onChangeStateStart != null)
            {
                onChangeStateStart.OnNext(next);
            }

            // 前のステートの終了待ち.

            if (prevState != null)
            {
                var exitYield = Exit(prevState, nextState.Type).ToYieldInstruction(true);

                while (!exitYield.IsDone)
                {
                    yield return null;
                }

                if (exitYield.HasError)
                {
                    Debug.LogException(exitYield.Error);
                }
            }

            // 現在のステートを更新.

            currentState = nextState;

            // ステートの開始.

            var enterYield = Enter(currentState, argument).ToYieldInstruction(true);

            while (!enterYield.IsDone)
            {
                yield return null;
            }

            if (enterYield.HasError)
            {
                Debug.LogException(enterYield.Error);
            }

            if (onChangeStateFinish != null)
            {
                onChangeStateFinish.OnNext(prev);
            }
        }

        /// <summary> 開始処理実行 </summary>
        private IEnumerator Enter<TArgument>(State<T> state, TArgument argument) where TArgument : StateArgument
        {
            int? finishedPriority = null;

            var count = 0;

            var enterFunctions = state.GetEnterFunctions();

            do
            {
                count = enterFunctions.Count;

                foreach (var enterFunction in enterFunctions)
                {
                    // 既に実行済みのプライオリティ以下の関数は呼び出ししない.
                    if (finishedPriority.HasValue && enterFunction.Key <= finishedPriority.Value) { continue; }

                    var functions = enterFunction.Value;

                    var observers = new List<IObservable<Unit>>();

                    foreach (var function in functions)
                    {
                        var func = function;

                        var observer = Observable.Defer(() => Observable.FromCoroutine(() => func(argument)).ObserveOnMainThread());

                        observers.Add(observer);
                    }

                    var enterYield = observers.WhenAll().ToYieldInstruction(true);

                    while (!enterYield.IsDone)
                    {
                        yield return null;
                    }

                    if (enterYield.HasError)
                    {
                        Debug.LogException(enterYield.Error);
                    }

                    finishedPriority = enterFunction.Key;

                    // 要素が増えていたら終了.
                    if (count != enterFunctions.Count) { break; }
                }

                enterFunctions = state.GetEnterFunctions();
            }
            while (count != enterFunctions.Count);
        }

        /// <summary> 終了処理実行 </summary>
        private IEnumerator Exit(State<T> state, T nextState)
        {
            int? finishedPriority = null;

            var count = 0;

            var exitFunctions = state.GetExitFunctions();

            do
            {
                count = exitFunctions.Count;

                foreach (var exitFunction in exitFunctions)
                {
                    // 既に実行済みのプライオリティ以下の関数は呼び出ししない.
                    if (finishedPriority.HasValue && exitFunction.Key <= finishedPriority.Value) { continue; }

                    var functions = exitFunction.Value;

                    var observers = new List<IObservable<Unit>>();

                    foreach (var function in functions)
                    {
                        var func = function;

                        var observer = Observable.Defer(() => Observable.FromCoroutine(() => func(nextState)).ObserveOnMainThread());

                        observers.Add(observer);
                    }

                    var exitYield = observers.WhenAll().ToYieldInstruction(true);

                    while (!exitYield.IsDone)
                    {
                        yield return null;
                    }

                    if (exitYield.HasError)
                    {
                        Debug.LogException(exitYield.Error);
                    }

                    finishedPriority = exitFunction.Key;

                    // 要素が増えていたら終了.
                    if (count != exitFunctions.Count) { break; }
                }

                exitFunctions = state.GetExitFunctions();
            }
            while (count != exitFunctions.Count);
        }

        /// <summary>
        /// 遷移開始時のイベント.
        /// </summary>
        /// <returns> 遷移先のState </returns>
        public IObservable<T> OnChangeStateStartAsObservable()
        {
            return onChangeStateStart ?? (onChangeStateStart = new Subject<T>());
        }

        /// <summary>
        /// 遷移完了時のイベント.
        /// </summary>
        /// <returns> 遷移元のState </returns>
        public IObservable<T> OnChangeStateFinishAsObservable()
        {
            return onChangeStateFinish ?? (onChangeStateFinish = new Subject<T>());
        }
    }
}
