
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using Cysharp.Threading.Tasks;
using Modules.ExternalResource;
using Modules.Sound;

namespace Modules.Scenario.Command
{
	public abstract class PlaySound : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		protected async UniTask<SoundElement> Play(SoundType soundType, string resourcePath, string cue)
		{
			var cueInfo = await ExternalResources.GetCueInfo(resourcePath, cue);

			if (cueInfo == null)
			{
				Debug.LogError($"Sound not found.\nResourcePath = {resourcePath}\nCue = {cue}");

				return null;
			}

			var soundElement = SoundManagement.Instance.Play(soundType, cueInfo);
			
			scenarioController.SoundController.Add(soundElement);

			return soundElement;
		}
	}
}

#endif
