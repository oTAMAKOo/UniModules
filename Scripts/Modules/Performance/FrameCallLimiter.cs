
using System;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Modules.Performance
{
	/// <summary> 1フレームで実行する後続処理数を制限. </summary>
	public sealed class FunctionFrameLimiter
	{
		//----- params -----

		//----- field -----

		private int frameCount = 0;

		private ulong current = default;

		private ulong max = default;

		private static int unityFrameCount = 0;

		private static IDisposable updateFrameCountDisposable = null;

		//----- property -----

		//----- method -----

		public FunctionFrameLimiter(ulong max)
		{
			this.max = max;

			if (updateFrameCountDisposable == null)
			{
				updateFrameCountDisposable = Observable.EveryUpdate().Subscribe(_ => unityFrameCount = Time.frameCount);
			}
		}

		public async UniTask Wait(ulong increment = 1, CancellationToken cancelToken = default)
		{
			try
			{
				while (true)
				{
					if (frameCount == unityFrameCount)
					{
						if (max <= current)
						{
							await UniTask.NextFrame(cancelToken);
						}
						else
						{
							break;
						}
					}
					else
					{
						frameCount = unityFrameCount;
						current = 0;
					}
				}

				current += increment;
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
		}

		public void Reset()
		{
			current = 0;
		}
	}
}
