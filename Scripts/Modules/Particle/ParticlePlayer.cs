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
        Loop
    }

    public enum LifcycleType
    {
        ParticleSystem,
        Manual,
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public partial class ParticlePlayer : MonoBehaviour
    {
        //----- params -----

        public class ParticleSystemInfo
        {
            public ParticleSystem ParticleSystem { get; private set; }
            public ParticleSystemRenderer Renderer { get; private set; }
            public ParticlePlayerSortingOrder SortingOrder { get; private set; }
            public float StartRotation { get; private set; }
            public float DefaultSpeed { get; private set; }

            public ParticleSystemInfo(ParticleSystem particleSystem)
            {
                ParticleSystem = particleSystem;
                StartRotation = particleSystem.main.startRotationMultiplier;
                Renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                SortingOrder = particleSystem.GetComponent<ParticlePlayerSortingOrder>();

                DefaultSpeed = particleSystem.main.simulationSpeed;
            }
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
        private LifcycleType lifecycleType = LifcycleType.ParticleSystem;
        [SerializeField]
        private int sortingOrder = 0;

        // ※ Animationから再生、中断する為のフィールド.

        [SerializeField]
        private bool play = false;
        [SerializeField]
        private bool pause = false;

        /// <summary>
        /// <see cref="lifecycleType" /> が <see cref="LifcycleType.Manual" /> の場合のエフェクト継続時間.
        /// <see cref="LifcycleType.Manual" /> 以外の場合は無視される.
        /// </summary>
        [SerializeField]
        private float lifeTime = 0f;

        // 現在の状態.
        private State currentState = State.Stop;

        // 現在の実行時間.
        private float currentTime = 0f;

        // 以前の状態.
        private State? prevState = null;

        // 再生速度(倍率).
        public float speedRate = 1f;

        // 管理対象.
        protected ParticleSystemInfo[] particleSystems = null;

        // 終了通知.
        private Subject<ParticlePlayer> onEnd = null;

        // アニメーションイベントフラグ.
        private bool played = false;
        private bool paused = false;

        // 再生中のエフェクト.
        private IObservable<Unit> playObservable = null;

        // 再生キャンセル用.
        private IDisposable playDisposable = null;

        private LifetimeDisposable disposable = new LifetimeDisposable();

        [NonSerialized]
        private bool isInitialized = false;

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

        public bool IsInitialized { get { return isInitialized; } }

        //----- method -----

        protected virtual void Initialize()
        {
            if(isInitialized) { return; }

            CollectContents();

            ResetContents();
            
            // AutoPlayじゃない場合は、初回の有効化時のみ、明示的に停止.
            // この時点で既にStateがPlayの場合は、明示的にPlayを叩いた状態なので、動作を止めない.
            if(!activateOnPlay && State != State.Play)
            {
                Stop();
            }

            isInitialized = true;
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

        /// <summary>
        /// <see cref="UniRx.Triggers.ObservableUpdateTrigger" /> で監視するとPrefabの変更になってしまうのでUpdateで監視を行う.
        /// </summary>
        void Update()
        {
            if(!isInitialized)
            {
                Initialize();
            }

            UpdateState();
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

            playObservable = Observable.FromCoroutine(() => PlayInternal()).Share();

            return playObservable;
        }

        private IEnumerator PlayInternal()
        {
            UnityUtility.SetActive(gameObject, true);

            // ヒエラルキー上で非アクティブでないか.
            if (!UnityUtility.IsActiveInHierarchy(gameObject))
            {
                Debug.LogErrorFormat("Animation can't play not active in hierarchy.\n{0}", gameObject.transform.name);
                yield break;
            }

            ApplySortingOrder(sortingOrder);

            while (true)
            {
                if (!UnityUtility.IsActiveInHierarchy(gameObject)) { break; }

                // 状態リセット.
                ResetContents();

                // 開始.
                SetState(State.Play);

                // 止まってたら開始.
                foreach (var ps in particleSystems.Where(x => !x.ParticleSystem.isPlaying))
                {
                    ps.ParticleSystem.Play();
                }

                // 再生速度.
                ApplySpeedRate();

                // 終了待ち.
                yield return Observable.FromCoroutine(() => WaitForEndOfAnimation()).ToYieldInstruction();

                if (endActionType != EndActionType.Loop) { break; }
            }

            EndAction();
        }

        // アニメーションの終了待ち.
        private IEnumerator WaitForEndOfAnimation()
        {
            if (!isInitialized) { yield break; }

            while (true)
            {
                if (State == State.Stop) { yield break; }

                if (State == State.Play)
                {
                    if (Application.isPlaying)
                    {
                        var time = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

                        // 時間更新.
                        FrameUpdate(time);

                        // ParticleSysmteのSimulate.
                        foreach (var ps in particleSystems)
                        {
                            if (UnityUtility.IsNull(ps.ParticleSystem)) { continue; }

                            if (!ps.ParticleSystem.gameObject.activeInHierarchy) { continue; }

                            ps.ParticleSystem.Simulate(time, false, false);
                        }

                        // 終了監視.
                        if (!IsAlive()) { break; }
                    }
                }

                yield return null;
            }
        }

        public void Stop(bool immediate = false, bool clear = true)
        {
            if (!isInitialized) { return; }

            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (UnityUtility.IsNull(ps.ParticleSystem)) { continue; }

                    var stopBehavior = immediate ? 
                        ParticleSystemStopBehavior.StopEmittingAndClear : 
                        ParticleSystemStopBehavior.StopEmitting;

                    ps.ParticleSystem.Stop(true, stopBehavior);
                }
            }

            Action onStopComplete = () =>
            {
                if (playDisposable != null)
                {
                    playDisposable.Dispose();
                    playDisposable = null;
                }

                currentTime = 0f;
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
                OnEndAsObservable().Subscribe(_ => onStopComplete()).AddTo(disposable.Disposable);
            }
        }

        private void ApplySpeedRate()
        {
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    var main = ps.ParticleSystem.main;

                    main.simulationSpeed *= speedRate;
                }
            }
        }

        private void ResetContents()
        {
            currentTime = 0f;

            prevState = null;
            pause = paused = false;
            play = played = false;

            SetState(State.Stop);

            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    var emission = ps.ParticleSystem.emission;
                    emission.enabled = true;

                    var main = ps.ParticleSystem.main;
                    main.simulationSpeed = ps.DefaultSpeed;

                    ps.ParticleSystem.Simulate(0f, false, true);
                }
            }
        }

        private void Clear()
        {
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
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
                    foreach (var ps in particleSystems)
                    {
                        ps.ParticleSystem.Play();
                    }
                }

                // Play -> Pause.
                if (state == State.Pause && prevState.Value == State.Play)
                {
                    foreach (var ps in particleSystems)
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

        protected void FrameUpdate(float time)
        {
            if (State != State.Play) { return; }

            UpdateState();

            currentTime += time;            
        }

        protected bool IsAlive()
        {
            var result = false;

            switch (lifecycleType)
            {
                case LifcycleType.ParticleSystem:
                    result = IsAliveParticleSystem();
                    break;

                case LifcycleType.Manual:
                    // 経過時間が生存期間より短ければ生存.
                    result = currentTime <= lifeTime;
                    break;
            }

            return result;
        }

        private bool IsAliveParticleSystem()
        {
            // 毎フレーム呼ばれる処理なのでLinqは使わない.
            foreach (var element in particleSystems)
            {
                var ps = element.ParticleSystem;

                if (UnityUtility.IsNull(ps)) { continue; }

                if (!UnityUtility.IsActiveInHierarchy(ps.gameObject)) { continue; }

                // ループエフェクトは常に生存.
                if (ps.main.loop) { return true; }

                // 再生時間より短いか.
                if (ps.time < ps.main.duration) { return true; }

                // 1つでも生きてるParticleSystemがいたら生存中.
                // ※ ParticleSystem.IsAlive()が正常に動かない為、particleCountで判定.
                if (0 < ps.particleCount) { return true; }
            }

            return false;
        }

        private void CollectContents()
        {
            var descendants = gameObject.DescendantsAndSelf().ToArray();

            particleSystems = descendants
                .OfComponent<ParticleSystem>()
                .Select(x => new ParticleSystemInfo(x))
                .ToArray();

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
            if (particleSystems != null)
            {
                foreach (var info in particleSystems)
                {
                    info.Renderer.sortingLayerID = (int)newValue;
                }
            }
        }

        private void ApplySortingOrder(int baseValue)
        {
            if (particleSystems != null)
            {
                foreach (var info in particleSystems)
                {
                    if (info.SortingOrder == null) { continue; }

                    info.SortingOrder.Apply(baseValue);
                }
            }
        }

        public IObservable<ParticlePlayer> OnEndAsObservable()
        {
            return onEnd ?? (onEnd = new Subject<ParticlePlayer>());
        }

        private void OnTransformChildrenChanged()
        {
            CollectContents();
        }
    }
}
