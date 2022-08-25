
#if ENABLE_VIVOX

using UnityEngine;
using System;
using System.Linq;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UniRx;
using VivoxUnity;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.Vivox
{
    public sealed partial class VivoxManager : Singleton<VivoxManager>
    {
        //----- params -----

		public static readonly string ConsoleEventName = "Vivox";
		public static readonly Color ConsoleEventColor = new Color(0.9f, 0.6f, 0.2f);

		private static readonly TimeSpan TokenExpirationDuration = TimeSpan.FromSeconds(90d);

		public enum ConnectType
		{
			TextOnly,
			AudioOnly,
			TextAndAudio,
		}

		public sealed class ParticipantStatusChangedData
		{
			public string username { get; private set; }
			public ChannelId channel{ get; private set; }
			public IParticipant participant{ get; private set; }

			public ParticipantStatusChangedData(string username, ChannelId channel, IParticipant participant)
			{
				this.username = username;
				this.channel = channel;
				this.participant = participant;
			}
		}

		public sealed class ParticipantValueChangedData
		{
			public string username { get; private set; }
			public ChannelId channel { get; private set; }
			public bool value { get; private set; }

			public ParticipantValueChangedData(string username, ChannelId channel, bool value)
			{
				this.username = username;
				this.channel = channel;
				this.value = value;
			}
		}

		public sealed class ParticipantValueUpdatedData
		{
			public string username { get; private set; }
			public ChannelId channel { get; private set; }
			public double value { get; private set; }

			public ParticipantValueUpdatedData(string username, ChannelId channel, double value)
			{
				this.username = username;
				this.channel = channel;
				this.value = value;
			}
		}

		//----- field -----

		private Client client = null;

		private AccountId accountId = null;

		private ILoginSession loginSession = null;

		private Subject<ILoginSession> onLoggingIn = null;
		private Subject<ILoginSession> onLoggedIn = null;
		private Subject<ILoginSession> onLoggingOut = null;
		private Subject<ILoginSession> onLoggedOut = null;
		private Subject<ConnectionRecoveryState> onRecoveryStateChanged = null;

		private Subject<IChannelSession> onChannelConnecting = null;
		private Subject<IChannelSession> onChannelConnected = null;
		private Subject<IChannelSession> onChannelDisconnecting = null;
		private Subject<IChannelSession> onChannelDisconnected = null;

		private Subject<ParticipantStatusChangedData> onAddedParticipant = null;
		private Subject<ParticipantStatusChangedData> onRemovedParticipant = null;
		private Subject<ParticipantValueChangedData> onDetectedParticipant = null;
		private Subject<ParticipantValueUpdatedData> onAudioEnergyChangedParticipant = null;
		
        //----- property -----

		/// <summary> ドメイン </summary>
		public string Domain { get; private set; }

		/// <summary> 発行者 </summary>
		public string Issuer { get; private set; }

		/// <summary> API エンドポイント </summary>
		public string Server { get; private set; }

		/// <summary> シークレットキー </summary>
		public string Token { get; private set; }

		public ILoginSession LoginSession { get { return loginSession; } }

		public LoginState LoginState { get; private set; }

		public IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels
		{
			get { return loginSession?.ChannelSessions; }
		}

		//----- method -----

		protected override void OnCreate()
		{
			client = new Client();
			
			client.Initialize();

			Observable.OnceApplicationQuit()
				.Subscribe(_ => Release())
				.AddTo(Disposable);
		}

		protected override void OnDispose()
		{
			Release();
		}

		public void Setup(string domain, string issuer, string server, string token)
		{
			Domain = domain;
			Issuer = issuer;
			Server = server;
			Token = token;

			Log($"Setup parameter\nDomain = {domain}\nIssuer = {issuer}\nServer = {server}\nToken = {token}");
		}

		public void Release()
		{
			if (client == null){ return; }

			Client.Cleanup();

			client.Uninitialize();

			client = null;

			Log("Client release complete");

		}
		
		public AccountId GetAccount(string uniqueId, string displayName)
		{
			return new AccountId(Issuer, uniqueId, Domain, displayName);
		}

		public void SetAccount(string uniqueId, string displayName)
		{
			accountId = GetAccount(uniqueId, displayName);
		}

		public async UniTask<bool> Login()
		{
			var result = false;

            loginSession = client.GetLoginSession(accountId);

            var serverUri = new Uri(Server);

            var token = loginSession.GetLoginToken(Token, TokenExpirationDuration);

			loginSession.PropertyChanged -= OnLoginStateChanged;
            loginSession.PropertyChanged += OnLoginStateChanged;

			var waitAsync = true;

			AsyncCallback callback = x =>
			{
				try
				{
					loginSession.EndLogin(x);

					RegisterLoginSessionEvent(loginSession);

					result = true;
				}
				catch (Exception e)
				{
					LogError(e.Message);

					loginSession.PropertyChanged -= OnLoginStateChanged;
					
					result = false;
				}
				finally
				{
					waitAsync = false;
				}
			};

            loginSession.BeginLogin(serverUri, token, callback);

			await UniTask.WaitWhile(() => waitAsync);

			return result;
		}

		public bool Logout()
		{
			if (loginSession == null || loginSession.State is LoginState.LoggingOut or LoginState.LoggedOut)
			{
				return false;
			}

			RemoveLoginSessionEvent(loginSession);

			loginSession.Logout();

            return true;
		}

		public ChannelId CreateChannelId(string channelName, ChannelType channelType, Channel3DProperties properties = null, string environmentId = null)
		{
			return new ChannelId(Issuer, channelName, Domain, channelType, properties, environmentId);
		}

		public async UniTask<bool> JoinChannel(ChannelId channelId, ConnectType connectType, bool switchTransmission = false)
		{
			if (loginSession.State != LoginState.LoggedIn)
			{
				LogError($"Cannot join a channel when not logged in.\nState : {loginSession.State}");

				return false;
			}

			var result = true;

			var waitAsync = true;
			
			var channelSession = loginSession.GetChannelSession(channelId);
			
			if (channelSession != null)
			{
				var token = channelSession.GetConnectToken(Token, TokenExpirationDuration);

				RegisterChannelSessionEvent(channelSession);
				
				AsyncCallback callback = x =>
				{
					try
					{
						channelSession.EndConnect(x);
					}
					catch (Exception e)
					{
						LogError($"Could not connect to voice channel: {e.Message}");

						channelSession.Disconnect();

						channelSession.Parent.DeleteChannelSession(channelSession.Channel);

						result = false;
					}
					finally
					{
						waitAsync = false;
					}
				};

				var connectAudio = connectType != ConnectType.TextOnly;
				var connectText = connectType != ConnectType.AudioOnly;

				channelSession.BeginConnect(connectAudio, connectText, switchTransmission, token, callback);

				await UniTask.WaitWhile(() => waitAsync);
			}

			return result;
		}

		public bool LeaveChannel(string channelName)
		{
			if (loginSession == null)
			{
				LogError($"Cannot leave a channel when not logged in.\nState : {loginSession.State}");

				return false;
			}

			var result = true;

			var channelSession = loginSession.ChannelSessions.FirstOrDefault(x => x.Channel.Name.Equals(channelName));

			if (channelSession != null)
			{
				channelSession.Disconnect();
			}
			else
			{
				result = false;
			}

			return result;
		}

		public void DisconnectAllChannels()
		{
			if (0 < ActiveChannels?.Count)
			{
				foreach (var channelSession in ActiveChannels)
				{
					channelSession?.Disconnect();
				}
			}
		}

		/// <summary> ログインセッションコールバック登録 </summary>
		private void RegisterLoginSessionEvent(ILoginSession loginSession)
		{
			if (loginSession == null){ return; }

			RemoveLoginSessionEvent(loginSession);

			loginSession.DirectedMessages.AfterItemAdded += OnDirectedMessagLogRecieved;
		}

		/// <summary> ログインセッションコールバック解除 </summary>
		private void RemoveLoginSessionEvent(ILoginSession loginSession)
		{
			if (loginSession == null){ return; }

			loginSession.DirectedMessages.AfterItemAdded -= OnDirectedMessagLogRecieved;
		}

		/// <summary> チャンネルセッションコールバック登録 </summary>
		private void RegisterChannelSessionEvent(IChannelSession channelSession)
		{
			if (channelSession == null){ return; }

			RemoveChannelSessionEvent(channelSession);

			channelSession.PropertyChanged += OnChannelPropertyChanged;
			channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
			channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
			channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
			channelSession.MessageLog.AfterItemAdded += OnMessageLogRecieved;
		}

		/// <summary> チャンネルセッションコールバック解除 </summary>
		private void RemoveChannelSessionEvent(IChannelSession channelSession)
		{
			if (channelSession == null){ return; }

			channelSession.PropertyChanged -= OnChannelPropertyChanged;
			channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
			channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
			channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
			channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;
		}

		#region Vivox Callbacks

		private static void ValidateArgs(object[] objs)
		{
			foreach (var obj in objs)
			{
				if (obj != null) { continue; }
				
				throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
			}
		}

		private void OnLoginStateChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			var propertyName = propertyChangedEventArgs.PropertyName;

			if (propertyName == nameof(ILoginSession.RecoveryState))
			{
				if (onRecoveryStateChanged != null)
				{
					if (LoginSession != null)
					{
						onRecoveryStateChanged.OnNext(LoginSession.RecoveryState);
					}
				}
					
				return;
			}

			if (propertyName != nameof(ILoginSession.State)) { return; }

			ValidateArgs(new object[] { sender, propertyChangedEventArgs });

			if (sender is ILoginSession loginSession)
			{
				LoginState = loginSession.State;

				switch (LoginState)
				{
					case LoginState.LoggingIn:
						{
							Log("Logging in");

							if (onLoggingIn != null)
							{
								onLoggingIn.OnNext(loginSession);
							}
						}
						break;

					case LoginState.LoggedIn:
						{
							Log("Connected to voice server and logged in.");

							if (onLoggedIn != null)
							{
								onLoggedIn.OnNext(loginSession);
							}
						}
						break;

					case LoginState.LoggingOut:
						{
							Log("Logging out");

							if (onLoggingOut != null)
							{
								onLoggingOut.OnNext(loginSession);
							}
						}
						break;

					case LoginState.LoggedOut:
						{
							Log("Logged out");

							loginSession.PropertyChanged -= OnLoginStateChanged;

							if (onLoggedOut != null)
							{
								onLoggedOut.OnNext(loginSession);
							}
						}
						break;
				}
			}
		}

		private void OnChannelStateChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			ValidateArgs(new object[] { sender, propertyChangedEventArgs });

			if (sender is IChannelSession channelSession)
			{
				if (propertyChangedEventArgs.PropertyName == nameof(IChannelSession.ChannelState))
				{
					switch (channelSession.ChannelState)
					{
						case ConnectionState.Disconnected:
							{
								channelSession.PropertyChanged -= OnChannelStateChanged;
								channelSession.Parent.DeleteChannelSession(channelSession.Channel);

								if (onChannelDisconnected != null)
								{
									onChannelDisconnected.OnNext(channelSession);
								}
							}
							break;

						case ConnectionState.Connecting:
							{
								if (onChannelConnecting != null)
								{
									onChannelConnecting.OnNext(channelSession);
								}
							}
							break;

						case ConnectionState.Connected:
							{
								if (onChannelConnected != null)
								{
									onChannelConnected.OnNext(channelSession);
								}
							}
							break;

						case ConnectionState.Disconnecting:
							{
								if (onChannelDisconnecting != null)
								{
									onChannelDisconnecting.OnNext(channelSession);
								}
							}
							break;
					}
				}
			}
		}

		private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			ValidateArgs(new object[] { sender, propertyChangedEventArgs });
			
			if (sender is IChannelSession channelSession)
			{
				var propertyName = propertyChangedEventArgs.PropertyName;

				var audioState = channelSession.AudioState;
				var textState = channelSession.TextState;

				if (propertyName == nameof(channelSession.AudioState) && audioState == ConnectionState.Disconnected)
				{
					Log($"Audio disconnected from : {channelSession.Key.Name}");

					foreach (var participant in channelSession.Participants)
					{
						if (onDetectedParticipant != null)
						{
							var data = new ParticipantValueChangedData(participant.Account.Name, channelSession.Channel, false);

							onDetectedParticipant.OnNext(data);
						}
					}
				}
				
				if (propertyName == nameof(channelSession.AudioState) || propertyName == nameof(channelSession.TextState))
				{
					if (audioState == ConnectionState.Disconnected && textState == ConnectionState.Disconnected)
					{
						Log($"Unsubscribing from: {channelSession.Key.Name}");
						
						RemoveChannelSessionEvent(channelSession);

						var user = client.GetLoginSession(accountId);
						
						user.DeleteChannelSession(channelSession.Channel);
					}
				}
			}
		}

		private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
	    {
	        ValidateArgs(new object[] { sender, keyEventArg });

			if (sender is VivoxUnity.IReadOnlyDictionary<string, IParticipant> source)
			{
		        var participant = source[keyEventArg.Key];
		        var username = participant.Account.Name;
		        var channel = participant.ParentChannelSession.Key;

				if (onAddedParticipant != null)
				{
					var data = new ParticipantStatusChangedData(username, channel, participant);

					onAddedParticipant.OnNext(data);
				}
			}
		}

	    private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
	    {
	        ValidateArgs(new object[] { sender, keyEventArg });

			if (sender is VivoxUnity.IReadOnlyDictionary<string, IParticipant> source)
			{
		        var participant = source[keyEventArg.Key];
		        var username = participant.Account.Name;
		        var channel = participant.ParentChannelSession.Key;
		        var channelSession = participant.ParentChannelSession;

		        if (participant.IsSelf)
		        {
		            Log($"Unsubscribing from: {channelSession.Key.Name}");
		            
					RemoveChannelSessionEvent(channelSession);
					
		            var user = client.GetLoginSession(accountId);

		            user.DeleteChannelSession(channelSession.Channel);
		        }
				
				if (onRemovedParticipant != null)
				{
					var data = new ParticipantStatusChangedData(username, channel, participant);

					onRemovedParticipant.OnNext(data);
				}
			}
	    }

		private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
		{
			ValidateArgs(new object[] { sender, valueEventArg });

			if (sender is VivoxUnity.IReadOnlyDictionary<string, IParticipant> source)
			{
				var participant = source[valueEventArg.Key];

				var username = valueEventArg.Value.Account.Name;
				var channel = valueEventArg.Value.ParentChannelSession.Key;
				var property = valueEventArg.PropertyName;

				switch (property)
				{
					case "SpeechDetected":
						{
							if (onDetectedParticipant != null)
							{
								var data = new ParticipantValueChangedData(username, channel, valueEventArg.Value.SpeechDetected);

								onDetectedParticipant.OnNext(data);
							}
						}
						break;

					case "AudioEnergy":
						{
							if (onAudioEnergyChangedParticipant != null)
							{
								var data = new ParticipantValueUpdatedData(username, channel, valueEventArg.Value.AudioEnergy);

								onAudioEnergyChangedParticipant.OnNext(data);
							}
						}
						break;
				}
			}
		}

		#endregion

		#region Event

		//------ LoginEvent ------
		
		/// <summary> ログイン開始イベント </summary>
		public IObservable<ILoginSession> OnLoggingInAsObservable()
		{
			return onLoggingIn ?? (onLoggingIn = new Subject<ILoginSession>());
		}

		/// <summary> ログイン完了イベント </summary>
		public IObservable<ILoginSession> OnLoggedInAsObservable()
		{
			return onLoggedIn ?? (onLoggedIn = new Subject<ILoginSession>());
		}

		/// <summary> ログアウト完了イベント </summary>
		public IObservable<ILoginSession> OnLoggingOutAsObservable()
		{
			return onLoggingOut ?? (onLoggingOut = new Subject<ILoginSession>());
		}

		/// <summary> 切断等、意図しないログアウトイベント </summary>
		public IObservable<ILoginSession> OnLoggedOutAsObservable()
		{
			return onLoggedOut ?? (onLoggedOut = new Subject<ILoginSession>());
		}

		/// <summary> リカバリ状態変化イベント </summary>
		public IObservable<ConnectionRecoveryState> OnRecoveryStateChangedAsObservable()
		{
			return onRecoveryStateChanged ?? (onRecoveryStateChanged = new Subject<ConnectionRecoveryState>());
		}
		
		//------ ChannelEvent ------

		/// <summary> チャンネル参加開始イベント </summary>
		public IObservable<IChannelSession> OnChannelConnectingAsObservable()
		{
			return onChannelConnecting ?? (onChannelConnecting = new Subject<IChannelSession>());
		}

		/// <summary> チャンネル参加イベント </summary>
		public IObservable<IChannelSession> OnChannelConnectedAsObservable()
		{
			return onChannelConnected ?? (onChannelConnected = new Subject<IChannelSession>());
		}

		/// <summary> チャンネル退出開始イベント </summary>
		public IObservable<IChannelSession> OnChannelDisconnectingAsObservable()
		{
			return onChannelDisconnecting ?? (onChannelDisconnecting = new Subject<IChannelSession>());
		}

		/// <summary> チャンネル退出イベント </summary>
		public IObservable<IChannelSession> OnChannelDisconnectedAsObservable()
		{
			return onChannelDisconnected ?? (onChannelDisconnected = new Subject<IChannelSession>());
		}

		//------ ParticipantEvent ------

		public IObservable<ParticipantStatusChangedData> OnAddedParticipantAsObservable()
		{
			return onAddedParticipant ?? (onAddedParticipant = new Subject<ParticipantStatusChangedData>());
		}

		public IObservable<ParticipantStatusChangedData> OnRemovedParticipantAsObservable()
		{
			return onRemovedParticipant ?? (onRemovedParticipant = new Subject<ParticipantStatusChangedData>());
		}

		public IObservable<ParticipantValueChangedData> OnDetectedParticipantAsObservable()
		{
			return onDetectedParticipant ?? (onDetectedParticipant = new Subject<ParticipantValueChangedData>());
		}

		public IObservable<ParticipantValueUpdatedData> OnAudioEnergyChangedParticipantAsObservable()
		{
			return onAudioEnergyChangedParticipant ?? (onAudioEnergyChangedParticipant = new Subject<ParticipantValueUpdatedData>());
		}
		
		#endregion

		#region Log

		private void Log(string text)
		{
			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, text);
		}

		private void LogError(string text)
		{
			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, text, LogType.Error);
		}

		#endregion
    }
}

#endif