
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.Net
{
    public static class NetworkConnection
	{
		//----- params -----

		private const float DefaultTimeOutSeconds = 15f;

		//----- field -----

		private static Subject<Unit> onNotReachable = null;

		//----- property -----

		public static float TimeOutSeconds { get; set; } = DefaultTimeOutSeconds;

		//----- method -----

		public static async UniTask WaitNetworkReachable(CancellationToken cancelToken = default)
		{
			var timeoutCancelTokenSource = new CancellationTokenSource();

			timeoutCancelTokenSource.CancelAfterSlim(TimeSpan.FromSeconds(TimeOutSeconds));

			var linkedCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, timeoutCancelTokenSource.Token);

			try
			{
				await WaitNetworkReachableInternal(linkedCancelTokenSource.Token);
			}
			catch (OperationCanceledException)
			{
				if (timeoutCancelTokenSource.IsCancellationRequested)
				{
					if (onNotReachable != null)
					{
						onNotReachable.OnNext(Unit.Default);
					}
				}
			}
		}

		private static async UniTask WaitNetworkReachableInternal(CancellationToken cancelToken)
		{
			while (Application.internetReachability == NetworkReachability.NotReachable)
			{
				await UniTask.NextFrame(cancelToken);
			}
		}

		public static IObservable<Unit> OnNotReachableAsObservable()
		{
			return onNotReachable ?? (onNotReachable = new Subject<Unit>());
		}
	}
}