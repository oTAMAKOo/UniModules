
using System.Threading;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

namespace Extensions
{
	public static class UniTaskExtensions
	{
		public static void Forget(this UniTask task, Component component)
		{
			task.ToObservable().Subscribe(_ => { }).AddTo(component);
		}

		public static void Forget(this UniTask task, GameObject gameObject)
		{
			task.ToObservable().Subscribe(_ => { }).AddTo(gameObject);
		}

		public static UniTask<T> ToUniTask<T>(this Observable<T> observable, CancellationToken cancellationToken = default)
		{
			return observable.FirstAsync(cancellationToken);
		}

		public static async UniTask ToUniTask(this Observable<Unit> observable, CancellationToken cancellationToken = default)
		{
			await observable.FirstAsync(cancellationToken);
		}
	}
}
