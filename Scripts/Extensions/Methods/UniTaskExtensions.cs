
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using R3;
using R3.Triggers;
using Cysharp.Threading.Tasks;

namespace Extensions
{
	public static class UniTaskExtensions
	{
		//----- Observe -----

		public static Observable<TProperty> ObserveEveryValueChanged<TSource, TProperty>(
			this TSource source,
			Func<TSource, TProperty> propertySelector,
			CancellationToken cancellationToken = default) where TSource : class
		{
			return Observable.EveryValueChanged(source, propertySelector, cancellationToken);
		}

		public static Observable<TProperty> ObserveEveryValueChanged<TSource, TProperty>(
			this TSource source,
			Func<TSource, TProperty> propertySelector,
			EqualityComparer<TProperty> equalityComparer,
			CancellationToken cancellationToken = default) where TSource : class
		{
			return Observable.EveryValueChanged(source, propertySelector, equalityComparer, cancellationToken);
		}

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

		//----- Retry -----

		public static Observable<T> OnErrorRetry<T, TException>(
			this Observable<T> source,
			Action<TException> onError,
			int retryMaxCount,
			float retryDelay) where TException : Exception
		{
			return OnErrorRetry(source, onError, retryMaxCount, TimeSpan.FromSeconds(retryDelay));
		}

		public static Observable<T> OnErrorRetry<T, TException>(
			this Observable<T> source,
			Action<TException> onError,
			int retryMaxCount,
			TimeSpan retryDelay) where TException : Exception
		{
			return Observable.Create<T>((observer, ct) =>
			{
				var retryCount = 0;

				void Subscribe()
				{
					source.Subscribe(
						onNext: value => observer.OnNext(value),
						onErrorResume: ex =>
						{
							if (ex is TException tex && retryCount < retryMaxCount)
							{
								retryCount++;
								onError(tex);
								Observable.Timer(retryDelay, TimeProvider.System)
									.Subscribe(_ => Subscribe())
									.RegisterTo(ct);
							}
							else
							{
								observer.OnCompleted(Result.Failure(ex));
							}
						},
						onCompleted: result => observer.OnCompleted(result))
					.RegisterTo(ct);
				}

				Subscribe();

				return default;
			});
		}

		//----- Conversion -----

		public static Observable<Unit> AsUnitObservable<T>(this Observable<T> source)
		{
			return source.Select(_ => Unit.Default);
		}

		public static async UniTask<T> ToUniTask<T>(this Observable<T> observable, CancellationToken cancellationToken = default)
		{
			return await observable.FirstAsync(cancellationToken);
		}

		public static async UniTask ToUniTask(this Observable<Unit> observable, CancellationToken cancellationToken = default)
		{
			await observable.FirstAsync(cancellationToken);
		}

		//----- Forget -----

		public static void Forget(this UniTask task, Component component)
		{
			task.AttachExternalCancellation(component.GetCancellationTokenOnDestroy()).Forget();
		}

		public static void Forget(this UniTask task, GameObject gameObject)
		{
			task.AttachExternalCancellation(gameObject.GetCancellationTokenOnDestroy()).Forget();
		}
	}
}
