
#if ENABLE_UNITY_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.TimeLine.Component
{
    public sealed class LabelClip : PlayableAsset, ITimelineClipAsset
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        public PlayableDirector PlayableDirector { get; private set; }

        //----- method -----

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            PlayableDirector = UnityUtility.GetComponent<PlayableDirector>(owner);

            var template = new LabelBehaviour();

            var playable = ScriptPlayable<LabelBehaviour>.Create(graph, template);
            
            return playable;
        }
    }
}

#endif