
using System;
using System.Threading;
using UnityEngine;
using R3;
using R3.Triggers;
using Cysharp.Threading.Tasks;

namespace Extensions
{
	public static class UniTaskExtensions
	{
		//----- Lifecycle -----

		public static Observable<T> TakeUntilDestroy<T>(this Observable<T> source, Component component)
		{
			return source.TakeUntil(component.OnDestroyAsObservable());
		}

		public static Observable<T> TakeUntilDestroy<T>(this Observable<T> source, GameObject gameObject)
		{
			return source.TakeUntil(gameObject.OnDestroyAsObservable());
		}

		public static Observable<T> TakeUntilDisable<T>(this Observable<T> source, Component component)
		{
			return source.TakeUntil(component.OnDisableAsObservable());
		}

		public static Observable<T> TakeUntilDisable<T>(this Observable<T> source, GameObject gameObject)
		{
			return source.TakeUntil(gameObject.OnDisableAsObservable());
		}

		//----- Do -----

		public static Observable<T> DoOnError<T>(this Observable<T> source, Action<Exception> onError)
		{
			return source.Do(onCompleted: result =>
			{
				if (result.IsFailure)
				{
					onError(result.Exception);
				}
			});
		}

		public static Observable<T> DoOnCompleted<T>(this Observable<T> source, Action onCompleted)
		{
			return source.Do(onCompleted: result =>
			{
				if (result.IsSuccess)
				{
					onCompleted();
				}
			});
		}

		public static Observable<T> DoOnTerminate<T>(this Observable<T> source, Action onTerminate)
		{
			return source.Do(onCompleted: _ => onTerminate());
		}

		public static Observable<T> DoOnCancel<T>(this Observable<T> source, Action onCancel)
		{
			return source.Do(onDispose: onCancel);
		}

		public static Observable<T> Finally<T>(this Observable<T> source, Action finallyAction)
		{
			return source.Do(onCompleted: _ => finallyAction());
		}

		//----- Conversion -----

		public static Observable<Unit> AsUnitObservable<T>(this Observable<T> source)
		{
			return source.Select(_ => Unit.Default);
		}

		public static UniTask<T> ToUniTask<T>(this Observable<T> observable, CancellationToken cancellationToken = default)
		{
			return observable.FirstAsync(cancellationToken);
		}

		public static async UniTask ToUniTask(this Observable<Unit> observable, CancellationToken cancellationToken = default)
		{
			await observable.FirstAsync(cancellationToken);
		}

		//----- Forget -----

		public static void Forget(this UniTask task, Component component)
		{
			task.ToObservable().Subscribe(_ => { }).AddTo(component);
		}

		public static void Forget(this UniTask task, GameObject gameObject)
		{
			task.ToObservable().Subscribe(_ => { }).AddTo(gameObject);
		}
	}
}
