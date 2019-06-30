
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using Extensions;

namespace Modules.TimeLine.Component
{
    [Serializable]
    public sealed class LoopBehaviour : PlayableBehaviour
    {
        //----- params -----

        //----- field -----

        private TimeLinePlayer timeLinePlayer = null;

        //----- property -----

        public PlayableDirector PlayableDirector { get; set; }
        public LoopInfo LoopInfo { get; set; }

        //----- method -----

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (timeLinePlayer == null)
            {
                timeLinePlayer = UnityUtility.GetComponent<TimeLinePlayer>(PlayableDirector.gameObject);
            }

            if (LoopInfo == null) { return; }

            // この呼び出し内でLoopフラグが書き換わる.
            timeLinePlayer.CheckLoop(LoopInfo);

            if (!LoopInfo.Loop) { return; }

            PlayableDirector.time -= playable.GetDuration();
        }
    }
}
