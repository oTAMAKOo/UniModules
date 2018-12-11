
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
	public class ExtendSoundManager : MonoBehaviour
	{
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        // サウンド再生システムをAdx2用に変更.
        public void OnCreateSoundSystem(SoundManager soundManager)
        {
            soundManager.System = new SoundManagerSystem();
        }
    }
}

#endif
