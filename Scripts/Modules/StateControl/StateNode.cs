﻿
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.StateControl
{
	public abstract class StateArgument { }

	public interface IStateNode<T> where T : Enum
	{
		T State { get;}
	}

	public abstract class StateNode<T> : IStateNode<T> where T : Enum
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public T State { get; private set; }

		//----- method -----

		public StateNode(T state)
		{
			State = state;
		}

		public virtual UniTask Enter(CancellationToken cancelToken = default)
		{
			return UniTask.CompletedTask;
		}

		public virtual UniTask Leave(CancellationToken cancelToken = default)
		{
			return UniTask.CompletedTask;
		}
	}

	public abstract class StateNode<T, TArgument> : IStateNode<T> where T : Enum where TArgument : StateArgument
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public T State { get; private set; }

		//----- method -----

		public StateNode(T state)
		{
			State = state;
		}

		public virtual UniTask Enter(TArgument argument, CancellationToken cancelToken = default)
		{
			return UniTask.CompletedTask;
		}

		public virtual UniTask Leave(CancellationToken cancelToken = default)
		{
			return UniTask.CompletedTask;
		}
	}
}
