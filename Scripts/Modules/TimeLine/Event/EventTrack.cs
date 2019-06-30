
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.TimeLine.Component
{
    [TrackColor(0.85f, 0.85f, 0.1f)]
    [TrackClipType(typeof(EventClip))]
    public sealed class EventTrack : TrackAsset
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject gameObject, int inputCount)
        {
            var events = new List<EventClip>();

            var clips = GetClips().ToArray();

            foreach (var clip in clips)
            {
                var playableAsset = clip.asset as EventClip;

                if (playableAsset != null)
                {
                    var info = new EventClip.Info()
                    {
                        name = clip.displayName,
                        start = clip.start,
                        end = clip.end,
                    };

                    playableAsset.EventInfo = info;

                    events.Add(playableAsset);
                }
            }

            var scriptPlayable = ScriptPlayable<EventMixerBehaviour>.Create(graph, inputCount);

            var timeLineEventMixer = scriptPlayable.GetBehaviour();

            timeLineEventMixer.Events = events.ToArray();

            return scriptPlayable;
        }
    }
}
