
using System;
using System.Threading;
using R3;
using Cysharp.Threading.Tasks;

namespace Modules.R3Extension
{
	public static class ObservableEx
	{
		public static Observable<T> FromUniTask<T>(Func<CancellationToken, UniTask<T>> taskFactory)
		{
			return Observable.FromAsync(async ct => await taskFactory(ct));
		}

		public static Observable<Unit> FromUniTask(Func<CancellationToken, UniTask> taskFactory)
		{
			return Observable.FromAsync(async ct => { await taskFactory(ct); return Unit.Default; });
		}
	}
}
