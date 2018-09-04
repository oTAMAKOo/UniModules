
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using Extensions;

namespace Modules.TimeLine.Component
{
    [Serializable]
    public class LoopClip : PlayableAsset, ITimelineClipAsset
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        //----- method -----

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LoopBehaviour>.Create(graph);

            var loopInfo = new LoopInfo(name) { Loop = true };

            var beheviour = playable.GetBehaviour();

            beheviour.PlayableDirector = owner.GetComponent<PlayableDirector>();

            beheviour.LoopInfo = loopInfo;

            return playable;
        }
    }
}
