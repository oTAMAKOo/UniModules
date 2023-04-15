
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.StateControl
{
    public sealed class StateController<T> : LifetimeDisposable where T : Enum
    {
        //----- params -----

		private sealed class StateEmptyArgument : StateArgument { }

		public sealed class ChangeStateInfo
		{
			public T from;
			public T to;
		}

		//----- field -----

        private IStateNode<T> currentNode = null;

        private Dictionary<T, IStateNode<T>> nodeTable = null;

        private CancellationTokenSource cancellationTokenSource = null;

        private Subject<ChangeStateInfo> onChangeStateStart = null;

        private Subject<ChangeStateInfo> onChangeStateFinish = null;

        //----- property -----

		public T Current
		{
			get
			{
				return currentNode != null ? currentNode.State : default;
			}
		}

        public bool IsExecute { get; private set; }

        //----- method -----

        public StateController()
        {
			nodeTable = new Dictionary<T, IStateNode<T>>();
        }

		/// <summary> ノードを登録 </summary>
		public void Register(T state, IStateNode<T> node)
		{
			nodeTable[state] = node;
		}

        /// <summary> ノードを取得 </summary>
        public IStateNode<T> Get(T state)
        {
			return nodeTable.GetValueOrDefault(state);
        }

        /// <summary> 登録されたステートをクリア </summary>
        public void Clear()
        {
            if (cancellationTokenSource != null)
            {
				cancellationTokenSource.Cancel();
				cancellationTokenSource = null;
            }

			nodeTable.Clear();

			currentNode = null;
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
                    if (cancellationTokenSource != null)
                    {
						cancellationTokenSource.Cancel();
						cancellationTokenSource = null;
                    }
                }
                else
                {
                    return;
                }
            }
			
			cancellationTokenSource = new CancellationTokenSource();

			ChangeState(next, argument).Forget();
        }
        
        private async UniTask ChangeState<TArgument>(T next, TArgument argument) where TArgument : StateArgument
        {
			IsExecute = true;

            var prevNode = currentNode;

            var nextNode = Get(next);

            if (nextNode == null)
            {
                throw new KeyNotFoundException($"This state is not registered. Type: {next}");
            }

			var changeStateInfo = new ChangeStateInfo()
			{
				from = prevNode.State, 
				to = nextNode.State
			};

            if (onChangeStateStart != null)
            {
                onChangeStateStart.OnNext(changeStateInfo);
            }

            // 前のステートの終了待ち.

            if (prevNode != null)
            {
				if (prevNode is StateNode<T>)
				{
					var node = prevNode as StateNode<T>;

					await node.Leave(cancellationTokenSource.Token);
				}
				else if (prevNode is StateNode<T, TArgument>)
				{
					var node = prevNode as StateNode<T, TArgument>;

					await node.Leave(cancellationTokenSource.Token);
				}
			}

            // 現在のステートを更新.

            currentNode = nextNode;

            // ステートの開始.

			if (currentNode is StateNode<T>)
			{
				var node = currentNode as StateNode<T>;

				await node.Enter(cancellationTokenSource.Token);
			}
			else if (currentNode is StateNode<T, TArgument>)
			{
				var node = currentNode as StateNode<T, TArgument>;

				await node.Enter(argument);
			}

            if (onChangeStateFinish != null)
            {
                onChangeStateFinish.OnNext(changeStateInfo);
            }

			IsExecute = false;
        }

		/// <summary> 遷移開始時のイベント </summary>
        public IObservable<ChangeStateInfo> OnChangeStateStartAsObservable()
        {
            return onChangeStateStart ?? (onChangeStateStart = new Subject<ChangeStateInfo>());
        }

        /// <summary> 遷移完了時のイベント </summary>
        public IObservable<ChangeStateInfo> OnChangeStateFinishAsObservable()
        {
            return onChangeStateFinish ?? (onChangeStateFinish = new Subject<ChangeStateInfo>());
        }
    }
}
