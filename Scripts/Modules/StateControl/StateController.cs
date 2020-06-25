
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

        private IStateNode<T> currentState = null;

        private Dictionary<T, IStateNode<T>> stateTable = null;

        private IDisposable changeStateDisposable = null;

        //----- property -----

        public T Current { get { return currentState != null ? currentState.State : default; } }

        //----- method -----

        public StateController()
        {
            lifetimeDisposable = new LifetimeDisposable();

            stateTable = new Dictionary<T, IStateNode<T>>();
        }

        /// <summary> 登録済みのノードを取得 </summary>
        public TStateNode GetNode<TStateNode>(T state) where TStateNode : class, IStateNode<T>
        {
            return stateTable.GetValueOrDefault(state) as TStateNode;
        }

        /// <summary> ステートを登録 </summary>
        public void Register(IStateNode<T> stateInstance)
        {
            var type = stateInstance.State;

            if (stateTable.ContainsKey(type))
            {
                throw new ArgumentException(string.Format("This state is already registered: {0}", type));
            }

            stateTable[type] = stateInstance;
        }

        /// <summary> 登録されたステートをクリア </summary>
        public void Reset()
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
        public void Request(T next)
        {
            Request(next, new StateEmptyArgument());
        }

        /// <summary> ステート変更を要求 </summary>
        public void Request<TArgument>(T next, TArgument argument) where TArgument : StateArgument, new()
        {
            if (changeStateDisposable != null)
            {
                changeStateDisposable.Dispose();
                changeStateDisposable = null;
            }

            changeStateDisposable = Observable.FromCoroutine(() => ChangeState(next, argument))
                .Subscribe(_ => changeStateDisposable = null)
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
