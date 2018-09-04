
using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.TimeLine.Component
{
    public class EventMixerBehaviour : PlayableBehaviour
    {
        //----- params -----

        //----- field -----

        public EventClip[] Events { get; set; }

        //----- property -----

        //----- method -----

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var time = playable.GetGraph().GetRootPlayable(0).GetTime();

            foreach (var item in Events)
            {
                item.UpdatePlayTime(time, info);
            }
        }
    }
}
