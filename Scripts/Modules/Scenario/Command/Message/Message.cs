
#if ENABLE_XLUA

using UnityEngine;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using Modules.TagTect;
using Modules.TimeUtil;

namespace Modules.Scenario.Command
{
	public abstract class Message : ScenarioCommand
	{
		//----- params -----

		private static readonly string[] WaitTag = { "[w]" };

		private static readonly string[] PageTag = { "[p]" };

		//----- field -----

		private float delayTime = 0f;

		private bool requestNext = false;

		private Subject<string> onRequestTextChange = null;

		//----- property -----

		public float DefaultCharDelayTime { get; set; } = 0.04f;

		protected abstract TagText TagText { get; }
		
		//----- method -----

		public async UniTask LuaCallback(string text, float? charDelayTime)
		{
			if (!charDelayTime.HasValue)
			{
				charDelayTime = DefaultCharDelayTime;
			}

			await SetMessageText(text, charDelayTime.Value);
		}

		public async UniTask SetMessageText(string message, float charDelayTime)
		{
			delayTime = 0f;

			requestNext = false;

			var text = ReceiveTextEdit(message);

			var current = new StringBuilder();

			var pages = text.Split(PageTag, StringSplitOptions.None);

			foreach (var page in pages)
			{
				current.Clear();

				if (onRequestTextChange != null)
				{
					onRequestTextChange.OnNext(string.Empty);
				}

				if (string.IsNullOrEmpty(page)) { continue; }

				var elements = page.Split(WaitTag, StringSplitOptions.None);
			
				foreach (var element in elements)
				{
					if (string.IsNullOrEmpty(element)) { continue; }

					TagText.SetText(element);

					requestNext = scenarioController.TimeScale.Value != TimeScale.DefaultTimeScale;

					for (var i = 0; i < TagText.Length; i++)
					{
						var t = TagText.Get(i);

						var now = current + t;

						if (onRequestTextChange != null)
						{
							onRequestTextChange.OnNext(now);
						}

						delayTime = 0;

						while (delayTime < charDelayTime)
						{
							if (requestNext){ break; }

							await UniTask.NextFrame();

							var timeScale = scenarioController.TimeScale.Value;

							delayTime += Time.deltaTime * timeScale;
						}
					}

					while (!requestNext)
					{
						await UniTask.NextFrame();
					}
					
					current.Append(TagText.Get());
				}
			}
		}

		public void RequestNext()
		{
			requestNext = true;
		}

		protected virtual string ReceiveTextEdit(string message){ return message; }

		public IObservable<string> OnRequestTextChangeAsObservable()
		{
			return onRequestTextChange ?? (onRequestTextChange = new Subject<string>());
		}
	}
}

#endif