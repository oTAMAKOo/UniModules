
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
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

			IsExecute = false;
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
            // 強制遷移で差し替わるため、この遷移のキャンセルトークンを保持する.
            var tokenSource = cancellationTokenSource;

            IsExecute = true;

            try
            {
                await ExecuteChangeState(next, argument, tokenSource.Token);
            }
            finally
            {
                // 後続の遷移が開始されている場合は、実行中の状態を維持する.
                if (cancellationTokenSource == tokenSource)
                {
                    IsExecute = false;
                }
            }
        }

        private async UniTask ExecuteChangeState<TArgument>(T next, TArgument argument, CancellationToken cancelToken) where TArgument : StateArgument
        {
            var prevNode = currentNode;

            var nextNode = Get(next);

            if (nextNode == null)
            {
                throw new KeyNotFoundException($"This state is not registered. Type: {next}");
            }

			var changeStateInfo = new ChangeStateInfo()
			{
				from = prevNode != null ? prevNode.State : default,
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

					await node.Leave(cancelToken);
				}
				else if (prevNode is StateNode<T, TArgument>)
				{
					var node = prevNode as StateNode<T, TArgument>;

					await node.Leave(cancelToken);
				}
			}

            // キャンセルされた遷移は、ステートを更新せずに終了する.
            if (cancelToken.IsCancellationRequested){ return; }

            // 現在のステートを更新.

            currentNode = nextNode;

            // ステートの開始.

			if (currentNode is StateNode<T>)
			{
				var node = currentNode as StateNode<T>;

				await node.Enter(cancelToken);
			}
			else if (currentNode is StateNode<T, TArgument>)
			{
				var node = currentNode as StateNode<T, TArgument>;

				await node.Enter(argument);
			}

            if (cancelToken.IsCancellationRequested){ return; }

            if (onChangeStateFinish != null)
            {
                onChangeStateFinish.OnNext(changeStateInfo);
            }
        }

		/// <summary> 遷移開始時のイベント </summary>
        public Observable<ChangeStateInfo> OnChangeStateStartAsObservable()
        {
            return onChangeStateStart ?? (onChangeStateStart = new Subject<ChangeStateInfo>());
        }

        /// <summary> 遷移完了時のイベント </summary>
        public Observable<ChangeStateInfo> OnChangeStateFinishAsObservable()
        {
            return onChangeStateFinish ?? (onChangeStateFinish = new Subject<ChangeStateInfo>());
        }
    }
}
