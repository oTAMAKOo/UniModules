
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public sealed class StateController<T> where T : Enum
    {
        //----- params -----

        private sealed class StateEmptyArgument : StateArgument { }

        //----- field -----

        private LifetimeDisposable lifetimeDisposable = null;

        private StateNodeBase<T> currentState = null;

        private Dictionary<T, StateNodeBase<T>> stateTable = null;

        private IDisposable changeStateDisposable = null;

        private bool isExecute = false;

        //----- property -----

        public T Current { get { return currentState != null ? currentState.State : default; } }

        //----- method -----

        public StateController()
        {
            lifetimeDisposable = new LifetimeDisposable();

            stateTable = new Dictionary<T, StateNodeBase<T>>();
        }

        /// <summary> ノードを取得 </summary>
        public StateNode<T> GetNode(T state)
        {
            StateNode<T> stateInstance = null;

            if (stateTable.ContainsKey(state))
            {
                stateInstance = stateTable[state] as StateNode<T>;
                
                // 登録済みのクラスと違う型で取得しようとしている.
                if (stateInstance == null)
                {
                    throw new Exception("Does not match registered class type.");
                }
            }
            else
            {
                stateInstance = new StateNode<T>();

                stateInstance.Initialize(state);

                stateTable[state] = stateInstance;
            }

            return stateInstance;
        }

        /// <summary> ノードを取得 </summary>
        public StateNode<T, TArgument> GetNode<TArgument>(T state) where TArgument : StateArgument, new()
        {
            StateNode<T, TArgument> stateInstance = null;

            if (stateTable.ContainsKey(state))
            {
                stateInstance = stateTable[state] as StateNode<T, TArgument>;

                // 登録済みのクラスと違う型で取得しようとしている.
                if (stateInstance == null)
                {
                    throw new Exception("Does not match registered class type.");
                }
            }
            else
            {
                stateInstance = new StateNode<T, TArgument>();

                stateInstance.Initialize(state);

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
                .AddTo(lifetimeDisposable.Disposable);
        }

        private IEnumerator ChangeState<TArgument>(T next, TArgument argument) where TArgument : StateArgument, new()
        {
            var prevState = currentState;

            var nextState = stateTable.GetValueOrDefault(next);

            // 前のステートの終了待ち.

            if (prevState != null)
            {
                var exitYield = prevState.Exit().ToYieldInstruction();

                while (!exitYield.IsDone)
                {
                    yield return null;
                }
            }

            currentState = nextState ?? throw new ArgumentException(string.Format("The specified state is not registered. State: {0}", next));

            // 引数を設定.

            var stateClass = currentState as StateNode<T, TArgument>;

            if (stateClass != null)
            {
                stateClass.SetArgument(argument);
            }

            // ステートの開始.

            var enterYield = currentState.Enter().ToYieldInstruction();

            while (!enterYield.IsDone)
            {
                yield return null;
            }
        }
    }
}
