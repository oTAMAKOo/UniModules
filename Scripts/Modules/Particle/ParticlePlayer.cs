﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using Unity.Linq;
using System;
using System.Collections;
using System.Linq;
using UniRx;
using Extensions;

using SortingLayer = Constants.SortingLayer;

namespace Modules.Particle
{
    public enum State
    {
        Play,
        Pause,
        Stop,
    }

    public enum EndActionType
    {
        None,
        Destroy,
        Deactivate,
        Loop,
    }

    public enum LifecycleControl
    {
        ParticleSystem,
        Manual,
    }

    public enum LifecycleType
    {
        None = 0,
        Birth,
        Alive,
        Death,
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed partial class ParticlePlayer : MonoBehaviour
    {
        //----- params -----

        public sealed class ParticleInfo
        {
            public ParticleSystem ParticleSystem { get; private set; }
            public ParticleSystemRenderer Renderer { get; private set; }
            public ParticlePlayerSortingOrder SortingOrder { get; private set; }
            public float StartRotation { get; private set; }
            public float DefaultSpeed { get; private set; }
            public bool IsSubemitter { get; private set; }
            public LifecycleType LifeCycle { get; set; }

            public ParticleInfo(ParticleSystem particleSystem, bool isSubemitter)
            {
                ParticleSystem = particleSystem;
                StartRotation = particleSystem.main.startRotationMultiplier;
                Renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                SortingOrder = particleSystem.GetComponent<ParticlePlayerSortingOrder>();
                DefaultSpeed = particleSystem.main.simulationSpeed;
                IsSubemitter = isSubemitter;
                LifeCycle = LifecycleType.None;
            }
        }

        [Serializable]
        public sealed class EventInfo
        {
            public enum EventTrigger
            {
                Time = 0,
                Birth,
                Alive,
                Death,
            }

            [SerializeField]
            public EventTrigger trigger = EventTrigger.Time;
            [SerializeField]
            public ParticleSystem target = null;
            [SerializeField]
            public float time = 0f;
            [SerializeField]
            public string message = null;
        }

        //----- field -----

        [SerializeField]
        private bool activateOnPlay = false;
        [SerializeField]
        private EndActionType endActionType = EndActionType.None;
        [SerializeField]
        private SortingLayer sortingLayer = SortingLayer.Default;
        [SerializeField]
        private bool ignoreTimeScale = false;
        [SerializeField]
        private LifecycleControl lifecycleControl = LifecycleControl.ParticleSystem;
        [SerializeField]
        private int sortingOrder = 0;
        [SerializeField]
        private EventInfo[] eventInfos = new EventInfo[0];

        // ※ Animationから再生、中断する為のフィールド.

        [SerializeField]
        private bool play = false;
        [SerializeField]
        private bool pause = false;

        [NonSerialized]
        private bool initialized = false;

        /// <summary>
        /// <see cref="lifecycleControl" /> が <see cref="LifecycleControl.Manual" /> の場合のエフェクト継続時間.
        /// <see cref="LifecycleControl.Manual" /> 以外の場合は無視される.
        /// </summary>
        [SerializeField]
        private float lifeTime = 0f;

        // 現在の状態.
        private State currentState = State.Stop;

        // 実行時間.
        private float currentTime = 0f;
        private float prevTime = 0f;

        // 以前の状態.
        private State? prevState = null;

        // 再生速度(倍率).
        private float speedRate = 1f;

        // 管理対象.
        private ParticleInfo[] particleInfos = null;

        // アニメーションイベントフラグ.
        private bool played = false;
        private bool paused = false;

        // 再生中のエフェクト.
        private IObservable<Unit> playObservable = null;

        // 再生キャンセル用.
        private IDisposable playDisposable = null;
        // 終了通知用.
        private IDisposable stopNotificationDisposable = null;

        private LifetimeDisposable disposable = new LifetimeDisposable();

        // イベント通知.
        private Subject<string> onEvent = null;
        // 終了通知.
        private Subject<ParticlePlayer> onEnd = null;

        //----- property -----

        public State State { get { return currentState; } }
        
        public float CurrentTime { get { return currentTime; } }

        public SortingLayer SortingLayer
        {
            get { return sortingLayer; }
            set
            {
                if (sortingLayer == value) return;

                sortingLayer = value;
                ApplySortingLayer(sortingLayer);
            }
        }

        public int SortingOrder
        {
            get { return sortingOrder; }
            set
            {
                if (sortingOrder == value) return;
                
                sortingOrder = value;
                ApplySortingOrder(sortingOrder);
            }
        }

        public EndActionType EndActionType
        {
            get { return endActionType; }

            set { endActionType = value; }
        }

        public bool Pause
        {
            get { return currentState == State.Pause; }

            set
            {
                if(value)
                {
                    SetState(State.Pause);
                }
                else
                {
                    if(prevState.HasValue)
                    {
                        SetState(prevState.Value);
                    }
                }
            }
        }

        public float SpeedRate
        {
            get { return speedRate; }

            set
            {
                speedRate = value;
                ApplySpeedRate();
            }
        }

        public bool IsInitialized { get { return initialized; } }

        //----- method -----

        private void Initialize()
        {
            if(initialized) { return; }

            CollectContents();

            ResetContents();
            
            // AutoPlayじゃない場合は、初回の有効化時のみ、明示的に停止.
            // この時点で既にStateがPlayの場合は、明示的にPlayを叩いた状態なので、動作を止めない.
            if(!activateOnPlay && State != State.Play)
            {
                Stop();
            }

            // 状態更新.
            Observable.EveryUpdate().Subscribe(_ => UpdateState()).AddTo(disposable.Disposable);

            initialized = true;
        }

        void OnEnable()
        {
            Initialize();

            CollectContents();

            if (activateOnPlay && Application.isPlaying)
            {
                playDisposable = Play().Subscribe().AddTo(disposable.Disposable);
            }
        }

        void OnDisable()
        {
            Stop(true, true);
        }

        private void UpdateState()
        {
            if (play != played)
            {
                if (play)
                {
                    playDisposable = Play().Subscribe().AddTo(disposable.Disposable);
                }
                else
                {
                    Stop();

                    // 即停止ではないので強制的にフラグを書き換え.
                    played = play;
                }
            }

            if (pause != paused)
            {
                if (State == State.Play && pause)
                {
                    Pause = true;
                }
                else if (State == State.Pause && !pause)
                {
                    Pause = false;
                }
            }
        }

        public IObservable<Unit> Play(bool restart = true)
        {
            if (!initialized)
            {
                Initialize();
            }

            if (restart)
            {
                Stop(true, true);
            }
            else
            {
                if (playObservable != null)
                {
                    return playObservable;
                }
            }

            // 再生.
            playObservable = Observable.FromMicroCoroutine(() => PlayInternal()).Share();

            return playObservable;
        }

        private IEnumerator PlayInternal()
        {
            UnityUtility.SetActive(gameObject, true);

            ApplySortingOrder(sortingOrder);

            while (true)
            {
                if (!UnityUtility.IsActiveInHierarchy(gameObject)) { break; }

                // 状態リセット.
                ResetContents();

                // 開始.
                SetState(State.Play);

                for (var i = 0; i < particleInfos.Length; i++)
                {
                    var particleInfo = particleInfos[i];

                    // 止まってたら開始.
                    if (!particleInfo.ParticleSystem.isPlaying)
                    {
                        particleInfo.ParticleSystem.Play();
                    }

                    particleInfo.LifeCycle = LifecycleType.None;
                }

                // 再生速度.
                ApplySpeedRate();

                // 終了待ち.
                var updateYield = Observable.FromMicroCoroutine(() => FrameUpdate()).ToYieldInstruction();

                while (!updateYield.IsDone)
                {
                    yield return null;
                }                

                if (endActionType != EndActionType.Loop) { break; }

                if (State == State.Stop) { break; }                
            }

            EndAction();
        }

        // 更新.
        private IEnumerator FrameUpdate()
        {
            while (true)
            {
                if (State == State.Stop) { break; }

                if (State == State.Play)
                {
                    if (Application.isPlaying)
                    {
                        var time = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

                        // 時間更新.
                        UpdateCurrentTime(time);

                        // イベント発行.
                        InvokeEvent();
                    }
                }

                // 終了監視.
                if (!IsAlive()) { break; }

                yield return null;
            }
        }

        public void Stop(bool immediate = false, bool clear = true)
        {
            if (!initialized) { return; }

            if (particleInfos != null)
            {
                foreach (var particleInfo in particleInfos)
                {
                    if (UnityUtility.IsNull(particleInfo.ParticleSystem)) { continue; }

                    var stopBehavior = immediate ? 
                        ParticleSystemStopBehavior.StopEmittingAndClear : 
                        ParticleSystemStopBehavior.StopEmitting;

                    particleInfo.ParticleSystem.Stop(true, stopBehavior);
                    particleInfo.LifeCycle = LifecycleType.None;
                }
            }

            if (stopNotificationDisposable != null)
            {
                stopNotificationDisposable.Dispose();
                stopNotificationDisposable = null;
            }

            Action onStopComplete = () =>
            {
                if (stopNotificationDisposable != null)
                {
                    stopNotificationDisposable.Dispose();
                    stopNotificationDisposable = null;
                }

                if (playDisposable != null)
                {
                    playDisposable.Dispose();
                    playDisposable = null;
                }

                currentTime = 0f;
                prevTime = 0f;

                playObservable = null;

                SetState(State.Stop);

                if (clear)
                {
                    Clear();
                }
            };

            if (immediate)
            {
                onStopComplete();
            }
            else
            {
                stopNotificationDisposable = OnEndAsObservable()
                    .Subscribe(_ => onStopComplete())
                    .AddTo(disposable.Disposable);
            }
        }

        private void ApplySpeedRate()
        {
            if (particleInfos != null)
            {
                foreach (var ps in particleInfos)
                {
                    var main = ps.ParticleSystem.main;

                    main.simulationSpeed *= speedRate;
                }
            }
        }

        private void ResetContents()
        {
            currentTime = 0f;
            prevTime = 0f;

            prevState = null;
            pause = paused = false;
            play = played = false;

            SetState(State.Stop);

            if (particleInfos != null)
            {
                foreach (var particleInfo in particleInfos)
                {
                    if (particleInfo.ParticleSystem == null) { continue; }

                    var emission = particleInfo.ParticleSystem.emission;
                    emission.enabled = true;

                    var main = particleInfo.ParticleSystem.main;
                    main.simulationSpeed = particleInfo.DefaultSpeed;

                    particleInfo.ParticleSystem.Simulate(0f, false, true);
                    particleInfo.LifeCycle = LifecycleType.None;
                }
            }
        }

        private void Clear()
        {
            if (particleInfos != null)
            {
                foreach (var ps in particleInfos)
                {
                    if (UnityUtility.IsNull(ps.ParticleSystem)) { continue; }

                    ps.ParticleSystem.Clear();
                }
            }
        }

        private void SetState(State state)
        {
            if (prevState.HasValue)
            {
                // Pause -> Play.
                if (state == State.Play && prevState.Value == State.Pause)
                {
                    foreach (var ps in particleInfos)
                    {
                        ps.ParticleSystem.Play();
                    }
                }

                // Play -> Pause.
                if (state == State.Pause && prevState.Value == State.Play)
                {
                    foreach (var ps in particleInfos)
                    {
                        ps.ParticleSystem.Pause(true);
                    }
                }
            }

            switch (state)
            {
                case State.Play:
                    play = true;
                    pause = false;
                    break;

                case State.Pause:
                    pause = true;
                    break;

                case State.Stop:
                    play = false;
                    pause = false;
                    break;
            }

            played = play;
            paused = pause;

            prevState = currentState;
            currentState = state;
        }

        private void UpdateCurrentTime(float time)
        {
            if (State != State.Play) { return; }

            UpdateState();

            prevTime = currentTime;
            currentTime += time;

            // Particle更新.
            for (var i = 0; i < particleInfos.Length; i++)
            {
                var particleInfo = particleInfos[i];

                var particleSystem = particleInfo.ParticleSystem;

                if (UnityUtility.IsNull(particleSystem)) { continue; }

                // フレーム更新.
                particleSystem.Simulate(time, false, false);

                // 状態更新.
                var playback = particleSystem.IsPlayback(particleInfo.IsSubemitter);

                switch (particleInfo.LifeCycle)
                {
                    case LifecycleType.None:
                        particleInfo.LifeCycle = playback ? LifecycleType.Birth : LifecycleType.None;
                        break;

                    case LifecycleType.Birth:
                        particleInfo.LifeCycle = playback ? LifecycleType.Alive : LifecycleType.Death;
                        break;

                    case LifecycleType.Alive:
                        particleInfo.LifeCycle = playback ? LifecycleType.Alive : LifecycleType.Death;
                        break;

                    case LifecycleType.Death:
                        particleInfo.LifeCycle = LifecycleType.None;
                        break;
                }
            }
        }

        /// <summary> イベント発行. </summary>
        private void InvokeEvent()
        {
            if (eventInfos == null) { return; }

            for (var i = 0; i < eventInfos.Length; i++)
            {
                var eventInfo = eventInfos[i];

                if (string.IsNullOrEmpty(eventInfo.message)) { continue; }

                var eventInvoke = false;

                var particleInfo = GetEventTargetParticle(eventInfo);

                switch (eventInfo.trigger)
                {
                    case EventInfo.EventTrigger.Time:
                        eventInvoke = prevTime < eventInfo.time && eventInfo.time <= currentTime;
                        break;

                    case EventInfo.EventTrigger.Birth:
                        eventInvoke = particleInfo != null && particleInfo.LifeCycle == LifecycleType.Birth;
                        break;

                    case EventInfo.EventTrigger.Alive:
                        eventInvoke = particleInfo != null && particleInfo.LifeCycle == LifecycleType.Alive;
                        break;

                    case EventInfo.EventTrigger.Death:
                        eventInvoke = particleInfo != null && particleInfo.LifeCycle == LifecycleType.Death;
                        break;
                }

                if (eventInvoke)
                {
                    if (onEvent != null)
                    {
                        onEvent.OnNext(eventInfo.message);
                    }
                }
            }
        }

        private ParticleInfo GetEventTargetParticle(EventInfo eventInfo)
        {
            if (eventInfo.target == null) { return null; }

            return particleInfos.FirstOrDefault(x => x.ParticleSystem == eventInfo.target);
        }

        public bool IsAlive()
        {
            var result = false;

            switch (lifecycleControl)
            {
                case LifecycleControl.ParticleSystem:
                    {
                        // 毎フレーム呼ばれる処理なのでLinqは使わない.
                        for (var i = 0; i < particleInfos.Length; i++)
                        {
                            var info = particleInfos[i];

                            result |= info.ParticleSystem.IsPlayback(info.IsSubemitter);
                        }
                    }
                    break;

                case LifecycleControl.Manual:
                    // 経過時間が生存期間より短ければ生存.
                    result = currentTime <= lifeTime;
                    break;
            }

            return result;
        }

        private void CollectContents()
        {
            var descendants = gameObject.DescendantsAndSelf().ToArray();

            var particleSystems = descendants.OfComponent<ParticleSystem>().ToArray();
            var subemitters = particleSystems.SelectMany(x => x.GetSubemitters()).ToArray();

            particleInfos = particleSystems.Select(x => new ParticleInfo(x, subemitters.Any(y => y == x))).ToArray();

            // Apply Settings.
            foreach (var ps in particleInfos)
            {
                var mainModule = ps.ParticleSystem.main;

                mainModule.playOnAwake = false;
            }

            ApplySortingLayer(sortingLayer);
            ApplySortingOrder(sortingOrder);

            ResetContents();
        }

        private void EndAction()
        {
            Stop();

            switch (endActionType)
            {
                case EndActionType.Deactivate:
                    if (Application.isPlaying)
                    {
                        UnityUtility.SetActive(gameObject, false);
                    }
                    break;

                case EndActionType.Destroy:
                    if (Application.isPlaying)
                    {
                        UnityUtility.SafeDelete(gameObject);
                    }
                    break;

                case EndActionType.Loop:
                    break;
            }

            playObservable = null;

            if (onEnd != null)
            {
                onEnd.OnNext(this);
            }
        }

        private void ApplySortingLayer(SortingLayer newValue)
        {
            if (particleInfos != null)
            {
                foreach (var info in particleInfos)
                {
                    info.Renderer.sortingLayerID = (int)newValue;
                }
            }
        }

        private void ApplySortingOrder(int baseValue)
        {
            if (particleInfos != null)
            {
                foreach (var info in particleInfos)
                {
                    if (info.SortingOrder == null) { continue; }

                    info.SortingOrder.Apply(baseValue);
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            CollectContents();
        }

        /// <summary> イベント発生通知 </summary>
        public IObservable<string> OnEventAsObservable()
        {
            return onEvent ?? (onEvent = new Subject<string>());
        }

        /// <summary> 終了通知 </summary>
        public IObservable<ParticlePlayer> OnEndAsObservable()
        {
            return onEnd ?? (onEnd = new Subject<ParticlePlayer>());
        }

        [ContextMenu("CollectContents")]
        private void RunCollectContents()
        {
            CollectContents();
        }
    }
}
