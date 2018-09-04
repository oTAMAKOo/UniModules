
using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.TimeLine.Component
{
    [Serializable]
    public class EventBehaviour : PlayableBehaviour
    {
        //----- params -----

        //----- field -----

        private EventClip timeLineEvent = null;
        private double prevTime = 0;

        private EventMethod[] onEnterMethods = null;
        private EventMethod[] onExitMethods = null;
        private EventMethod[] onStayMethods = null;

        //----- property -----

        public TimeLinePlayer TimeLinePlayer { get; private set; }

        //----- method -----

        public void SetTimeLineEvent(EventClip timeLineEvent)
        {
            this.timeLineEvent = timeLineEvent;
           
            if (timeLineEvent.EventMethods != null)
            {
                var allEventMethods = timeLineEvent.EventMethods;

                onEnterMethods = allEventMethods.Where(x => x.EventType == EventType.Enter).ToArray();
                onExitMethods = allEventMethods.Where(x => x.EventType == EventType.Exit).ToArray();
                onStayMethods = allEventMethods.Where(x => x.EventType == EventType.Stay).ToArray();
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            TimeLinePlayer = GetTimeLinePlayer(playable);
        }

        public void UpdatePlayTime(double time, FrameData info)
        {
            var eventInfo = timeLineEvent.EventInfo;

            if (info.frameId == 0 || 0 < info.deltaTime)
            {
                if (prevTime < eventInfo.start && eventInfo.start <= time)
                {
                    if (onEnterMethods != null)
                    {
                        foreach (var onEnterMethod in onEnterMethods)
                        {
                            onEnterMethod.Invoke();
                        }
                    }
                }
                else if (prevTime < eventInfo.end && eventInfo.end <= time)
                {
                    if (onExitMethods != null)
                    {
                        foreach (var onExitMethod in onExitMethods)
                        {
                            onExitMethod.Invoke();
                        }
                    }
                }
                else if (eventInfo.start <= time && time < eventInfo.end)
                {
                    if (onStayMethods != null)
                    {
                        foreach (var onStayMethod in onStayMethods)
                        {
                            onStayMethod.Invoke();
                        }
                    }
                }

                prevTime = time;
            }
        }

        private static TimeLinePlayer GetTimeLinePlayer(Playable playable)
        {
            var director = playable.GetGraph().GetResolver() as PlayableDirector;

            if (director == null) { return null; }

            var timeLinePlayer = UnityUtility.GetComponent<TimeLinePlayer>(director.gameObject);

            return timeLinePlayer;
        }
    }
}
