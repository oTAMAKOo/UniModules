
#if ENABLE_VIVOX

using UnityEngine;
using System;
using System.Linq;
using UniRx;
using VivoxUnity;
using Extensions;

namespace Modules.Vivox
{
	public partial class VivoxManager
	{
		//----- params -----

		public const float DefaultUpdatePositionInterval = 1f;

		public struct PositionalData
		{
			public Vector3 speakerPos;
			public Vector3 listenerPos;
			public Vector3 listenerAtOrient;
			public Vector3 listenerUpOrient;
		}

		//----- field -----

		private float updateTimer = 0f;
		
		private PositionalData prevPositional = default;

		private IDisposable updatePositionDisposable = null;

		private Subject<Unit> onRequestUpdatePositional = null;

		//----- property -----

		public PositionalData CurrentPositional { get; private set; }

		public IAudioDevices AudioInputDevices { get { return client.AudioInputDevices; } }

		public IAudioDevices AudioOutputDevices { get { return client.AudioOutputDevices; } }

		/// <summary> 位置更新インターバル </summary>
		public float UpdatePositionInterval { get; set; } = DefaultUpdatePositionInterval;

		//----- method -----

		public void BeginPositionalUpdate()
		{
			if (updatePositionDisposable != null)
			{
				updatePositionDisposable.Dispose();
				updatePositionDisposable = null;
			}

			updatePositionDisposable = Observable.EveryLateUpdate()
				.Subscribe(_ => OnUpdate3DPosition())
				.AddTo(Disposable);
		}

		private void OnUpdate3DPosition()
		{
			updateTimer += Time.deltaTime;

			if (updateTimer < UpdatePositionInterval){ return; }

			updateTimer = 0;

			// 情報更新リクエスト発行.

			if (onRequestUpdatePositional != null)
			{
				onRequestUpdatePositional.OnNext(Unit.Default);
			}
			
			// 位置が同じ場合更新しない.
			if (CurrentPositional.Equals(prevPositional)){ return; }
			
			var positionalChannelSession = loginSession.ChannelSessions
				.Where(x => x.AudioState == ConnectionState.Connected)
				.Where(x => x.Channel.Type == ChannelType.Positional)
				.ToArray();

			foreach (var channelSession in positionalChannelSession)
			{
				var speakerPos = CurrentPositional.speakerPos;
				var listenerPos = CurrentPositional.listenerPos;
				var listenerAtOrient = CurrentPositional.listenerAtOrient;
				var listenerUpOrient = CurrentPositional.listenerUpOrient;

				channelSession.Set3DPosition(speakerPos, listenerPos, listenerAtOrient, listenerUpOrient);
			}

			prevPositional = CurrentPositional;
		}

		/// <summary> 3Dサウンドの情報設定 </summary>
		public void SetPositionalData(PositionalData positionalData)
		{
			CurrentPositional = positionalData;
		}

		#region Vivox Callbacks


		#endregion

		#region Event

		public IObservable<Unit> OnRequestUpdatePositionalAsObservable()
		{
			return onRequestUpdatePositional ?? (onRequestUpdatePositional = new Subject<Unit>());
		}

		#endregion
	}
}

#endif

