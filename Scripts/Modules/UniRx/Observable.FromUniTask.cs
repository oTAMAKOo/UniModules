﻿
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniRx
{
    public static class ObservableEx
    {
		public static IObservable<TResult> FromUniTask<TResult>(Func<CancellationToken, UniTask<TResult>> functionAsync)
		{
			return Observable.Defer(() => StartAsync(functionAsync));
		}

		private static IObservable<TResult> StartAsync<TResult>(Func<CancellationToken, UniTask<TResult>> functionAsync)
        {
            var cancellable = new CancellationDisposable();

            var task = default(UniTask<TResult>);

            try
            {
                task = functionAsync(cancellable.Token);
            }
            catch (Exception exception)
            {
                return Observable.Throw<TResult>(exception);
            }

            var result = task.ToObservable();

            return Observable.Create<TResult>(observer =>
            {
                var subscription = result.Subscribe(observer);
                return new CompositeDisposable(cancellable, subscription);
            });
        }

		public static IObservable<Unit> FromUniTask(Func<CancellationToken, UniTask> actionAsync)
		{
			return Observable.Defer(() => StartAsync(actionAsync));
		}

        private static IObservable<Unit> StartAsync(Func<CancellationToken, UniTask> actionAsync)
        {
            var cancellable = new CancellationDisposable();

            var task = default(UniTask);
            try
            {
                task = actionAsync(cancellable.Token);
            }
            catch (Exception exception)
            {
                return Observable.Throw<Unit>(exception);
            }

            var result = task.ToObservable().AsUnitObservable();

            return Observable.Create<Unit>(observer =>
            {
                var subscription = result.Subscribe(observer);

                return new CompositeDisposable(cancellable, subscription);
            });
        }
    }
}
