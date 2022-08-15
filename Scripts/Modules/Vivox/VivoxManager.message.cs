
#if ENABLE_VIVOX

using UnityEngine;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using VivoxUnity;
using Extensions;

namespace Modules.Vivox
{
	public partial class VivoxManager
	{
		//----- params -----

		//----- field -----

		private Subject<IChannelTextMessage> onReceivedMessage = null;
		private Subject<IDirectedTextMessage> onReceivedDirectedMessage = null;

		//----- property -----

		//----- method -----

		/// <summary> チャンネルメッセージ送信 </summary>
		public async UniTask SendMessage(ChannelId channel, string messageToSend, string applicationStanzaNamespace = null, string applicationStanzaBody = null)
	    {
	        if (ChannelId.IsNullOrEmpty(channel))
	        {
	            throw new ArgumentException("Must provide a valid ChannelId");
	        }

	        if (string.IsNullOrEmpty(messageToSend))
	        {
	            throw new ArgumentException("Must provide a message to send");
	        }

	        var channelSession = loginSession.GetChannelSession(channel);

			if (channelSession != null)
			{
				var waitAsync = true;

				AsyncCallback callback = x =>
				{
					try
					{
						channelSession.EndSendText(x);
					}
					catch (Exception e)
					{
						LogError($"SendTextMessage failed with exception {e.Message}");
					}
					finally
					{
						waitAsync = false;
					}
				};

				channelSession.BeginSendText(null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, callback);

				await UniTask.WaitWhile(() => waitAsync);
			}
	    }

		/// <summary> ダイレクトメッセージ送信 </summary>
		public async UniTask SendDirectedMessage(AccountId targetAccount, string messageToSend, string applicationStanzaNamespace = null, string applicationStanzaBody = null)
	    {
			if (string.IsNullOrEmpty(messageToSend))
			{
				throw new ArgumentException("Must provide a message to send");
			}

	        if (AccountId.IsNullOrEmpty(targetAccount))
	        {
	            throw new ArgumentException("Must provide a account to send");
	        }

	        var waitAsync = true;

			AsyncCallback callback = x =>
			{
				try
				{
					loginSession.EndSendDirectedMessage(x);
				}
				catch (Exception e)
				{
					LogError($"SendDirectedMessage threw {e}");
				}
				finally
				{
					waitAsync = false;
				}
			};

			loginSession.BeginSendDirectedMessage(targetAccount, null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, callback);

			await UniTask.WaitWhile(() => waitAsync);
	    }

		#region Vivox Callbacks

		/// <summary> チャンネルメッセージ受信コールバック </summary>
		private void OnMessageLogRecieved(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
		{
			ValidateArgs(new object[] { sender, textMessage });

			var channelTextMessage = textMessage.Value;

			if (onReceivedMessage != null)
			{
				onReceivedMessage.OnNext(channelTextMessage);
			}
		}

		/// <summary> ダイレクトメッセージ受信コールバック </summary>
		private void OnDirectedMessagLogRecieved(object sender, QueueItemAddedEventArgs<IDirectedTextMessage> textMessage)
		{
			ValidateArgs(new object[] { sender, textMessage });

			var directedTextMessage = textMessage.Value;

			if (onReceivedMessage != null)
			{
				onReceivedDirectedMessage.OnNext(directedTextMessage);
			}
		}

		#endregion

		#region Event

		public IObservable<IChannelTextMessage> OnReceivedMessageAsObservable()
		{
			return onReceivedMessage ?? (onReceivedMessage = new Subject<IChannelTextMessage>());
		}

		public IObservable<IDirectedTextMessage> OnReceivedDirectedMessageAsObservable()
		{
			return onReceivedDirectedMessage ?? (onReceivedDirectedMessage = new Subject<IDirectedTextMessage>());
		}

		#endregion
	}
}

#endif

