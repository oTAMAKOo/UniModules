﻿
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_XLUA

using UnityEngine;
using Cysharp.Threading.Tasks;
using Modules.ExternalAssets;
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
			var cueInfo = await ExternalAsset.GetCueInfo(resourcePath, cue);

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
