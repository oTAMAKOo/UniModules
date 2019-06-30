
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.TimeLine.Component
{
    [Serializable]
    public sealed class EventClip : PlayableAsset, ITimelineClipAsset
    {
        //----- params -----

        public class Info
        {
            public string name { get; set; }
            public double start { get; set; }
            public double end { get; set; }
        }

        //----- field -----

        [SerializeField]
        private EventMethod[] methods = null;

        private EventBehaviour playableBehaviour = null;

        //----- property -----

        public EventMethod[] EventMethods { get { return methods; } }

        public PlayableDirector PlayableDirector { get; private set; }

        public ClipCaps clipCaps { get { return ClipCaps.None; } }

        public Info EventInfo { get; set; }

        //----- method -----

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            PlayableDirector = UnityUtility.GetComponent<PlayableDirector>(owner);

            if (methods != null)
            {
                foreach (var method in methods)
                {
                    method.Setup(PlayableDirector);
                }
            }

            var template = new EventBehaviour();

            var playable = ScriptPlayable<EventBehaviour>.Create(graph, template);

            // PlayableBehaviour setup.
            playableBehaviour = playable.GetBehaviour();
            playableBehaviour.SetTimeLineEvent(this);

            return playable;
        }

        public void UpdatePlayTime(double time, FrameData info)
        {
            playableBehaviour.UpdatePlayTime(time, info);
        }
    }
}
