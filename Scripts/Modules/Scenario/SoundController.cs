
#if ENABLE_CRIWARE_ADX

using System.Collections.Generic;
using UniRx;
using Modules.Sound;
using Extensions;

namespace Modules.Scenario
{
    public sealed class SoundController : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

		private List<SoundElement> soundsElements = null;

        //----- property -----

		public SoundElement[] Elements { get { return soundsElements.ToArray(); } }

        //----- method -----

		public SoundController()
		{
			var soundManagement = SoundManagement.Instance;
			
			soundsElements = new List<SoundElement>();

			soundManagement.OnReleaseAsObservable()
				.Subscribe(x => Remove(x))
				.AddTo(Disposable);
		}

		public void Add(SoundElement soundElement)
		{
			soundsElements.Add(soundElement);
		}

		public void Remove(SoundElement soundElement)
		{
			if(soundsElements.Contains(soundElement))
			{
				soundsElements.Remove(soundElement);
			}
		}

		public void Clear()
		{
			soundsElements.Clear();
		}
	}
}

#endif
