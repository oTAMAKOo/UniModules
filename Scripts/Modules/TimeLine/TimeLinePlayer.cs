
#if ENABLE_UNITY_TIMELINE

using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.TimeLine.Component;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Modules.TimeLine
{
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class TimeLinePlayer : MonoBehaviour
    {
        //----- params -----

        public enum State
        {
            None = 0,

            Play,
            Pause,
            Finish,
        }

        //----- field -----

        private PlayableDirector playableDirector = null;
        private IDisposable playingDisposable = null;

        private State state = State.None;
        private double timeKeeper = 0;
        private double speed = 1f;
        
        private Dictionary<string, double> labels = null;

        private Subject<LoopInfo> onLoopCheck = null;

        private bool initialize = false;

        //----- property -----

        public double CurrentTime { get { return playableDirector.time; } }

        public double Speed { get { return speed; } }

        //----- method -----

        private void Initialize()
        {
            if (initialize) { return; }

            playableDirector = UnityUtility.GetComponent<PlayableDirector>(gameObject);

            labels = new Dictionary<string, double>();

            var labelClips = ((TimelineAsset)playableDirector.playableAsset)
                    .GetOutputTracks()
                    .Where(x => x is LabelTrack)
                    .SelectMany(x => x.GetClips())
                    .ToArray();

            foreach (var labelClip in labelClips)
            {
                if (labels.ContainsKey(labelClip.displayName)) { continue; }

                labels.Add(labelClip.displayName, labelClip.start);
            }

            initialize = true;
        }
        
        /// <summary> 再生. </summary> 
        public IObservable<Unit> Play(double time = 0, bool resetIfPlaying = true)
        {
            Initialize();

            if (state == State.None)
            {
                Stop();
            }

            if (state == State.Play || state == State.Pause)
            {
                if (resetIfPlaying)
                {
                    Stop();
                }
            }

            if (playingDisposable == null)
            {
                playingDisposable = Observable.EveryUpdate()
                    .Subscribe(_ => CheckFinish())
                    .AddTo(this);
            }

            state = State.Play;

            SetTime(time);

            playableDirector.Play();

            return Observable.FromMicroCoroutine(() => WaitFinish());
        }

        private IEnumerator WaitFinish()
        {
            while (state != State.Finish)
            {
                yield return null;
            }

            if (playingDisposable != null)
            {
                playingDisposable.Dispose();
                playingDisposable = null;
            }

            state = State.None;
        }

        /// <summary> 一時停止. </summary> 
        public void Pause()
        {
            Initialize();

            playableDirector.Pause();

            state = State.Pause;
        }


        /// <summary> 停止. </summary> 
        public void Stop()
        {
            Initialize();

            if (playingDisposable != null)
            {
                playingDisposable.Dispose();
                playingDisposable = null;
            }

            timeKeeper = 0;

            playableDirector.Stop();

            state = State.Finish;
        }

        /// <summary> Labelにジャンプして再生. </summary> 
        public IObservable<Unit> GotoAndPlay(string label)
        {
            if (string.IsNullOrEmpty(label)) { return Observable.ReturnUnit(); }

            Initialize();

            if (!labels.ContainsKey(label)) { return Observable.ReturnUnit(); }

            var time = labels.GetValueOrDefault(label);

            return Play(time);
        }

        /// <summary> Labelにジャンプして停止. </summary>     
        public void GotoAndStop(string label)
        {
            if (string.IsNullOrEmpty(label)) { return; }

            Initialize();

            if (!labels.ContainsKey(label)) { return; }

            var time = labels.GetValueOrDefault(label);

            SetTime(time);
        }

        /// <summary> 再生速度変更. </summary>
        public void SetSpeed(double speed)
        {
            this.speed = speed;

            Initialize();

            if (state == State.Play)
            {
                playableDirector.Pause();
            }

            var playableGraph = playableDirector.playableGraph;

            var count = playableGraph.GetRootPlayableCount();

            for (var i = 0; i < count; i++)
            {
                var playable = playableGraph.GetRootPlayable(i);

                playable.SetSpeed(speed);
            }

            if (state == State.Play)
            {
                playableDirector.Play();
            }
        }

        /// <summary> 時間を直接変更. </summary>
        public void SetTime(double time)
        {
            Initialize();

            playableDirector.Evaluate();

            playableDirector.time = time;

            playableDirector.Evaluate();
        }

        /// <summary> ループ判定実行. </summary>
        public void CheckLoop(LoopInfo info)
        {
            if (onLoopCheck != null)
            {
                onLoopCheck.OnNext(info);
            }
        }

        private void CheckFinish()
        {
            if (playableDirector.state == PlayState.Paused && playableDirector.duration <= timeKeeper)
            {
                state = State.Finish;
            }

            if (0 < playableDirector.time)
            {
                timeKeeper = playableDirector.time + Time.deltaTime;
            }
        }

        public IObservable<LoopInfo> OnLoopCheckAsObservable()
        {
            return onLoopCheck ?? (onLoopCheck = new Subject<LoopInfo>());
        }
    }
}

#endif