
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Net
{
    public static class NetworkConnection
	{
		//----- params -----

		private const float DefaultTimeOutSeconds = 15f;

		//----- field -----

		//----- property -----

		public static float TimeOutSeconds { get; set; } = DefaultTimeOutSeconds;

		//----- method -----

		public static async UniTask WaitNetworkReachable(CancellationToken cancelToken)
		{
			var timeoutController = new TimeoutController();

			var timeOutCancelToken = timeoutController.Timeout(TimeSpan.FromSeconds(TimeOutSeconds));

			var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeOutCancelToken, cancelToken);

			await WaitNetworkReachableInternal(cancelTokenSource.Token);
		}

		private static async UniTask WaitNetworkReachableInternal(CancellationToken cancelToken)
		{
			while (Application.internetReachability == NetworkReachability.NotReachable)
			{
				await UniTask.NextFrame(cancelToken);
			}
		}
	}
}