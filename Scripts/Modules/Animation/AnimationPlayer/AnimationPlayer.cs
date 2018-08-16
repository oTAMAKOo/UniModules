﻿
using UnityEngine;
using System;
using System.Collections;
using UniRx;
using Extensions;
using Modules.StateMachine;

namespace Modules.Animation
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class AnimationPlayer : MonoBehaviour, IStateMachineEventHandler
    {
        //----- params -----

        private class AnimatorInfo
        {
            public Animator Animator { get; private set; }
            public RuntimeAnimatorController AnimatorController { get; private set; }
            public AnimationClip[] Clips { get; private set; }
            public float DefaultSpeed { get; private set; }

            public AnimatorInfo(Animator animator)
            {
                Animator = animator;
                AnimatorController = Animator.runtimeAnimatorController;
                Clips = AnimatorController != null ? AnimatorController.animationClips : new AnimationClip[0];
                DefaultSpeed = Animator.speed;
            }
        }

        //----- field -----

        [SerializeField]
        private RuntimeAnimatorController animatorController = null;
        [SerializeField]
        private EndActionType endActionType = EndActionType.None;
        [SerializeField]
        private bool ignoreTimeScale = false;

        private AnimatorInfo animatorInfo = null;

        private string animationName = null;

        // 現在の状態.
        private State currentState = State.Stop;

        // ステート遷移待ちフラグ.
        private bool waitStateTransition = false;

        // 終了通知.
        private Subject<AnimationPlayer> onEndAnimation = null;

        // StateMachineイベント.
        private Subject<StateMachineEvent> onStateMachineEvent = null;

        // アニメーションイベント.
        private Subject<string> onAnimationEvent = null;

        // ポーズ中か.
        private bool isPause = false;

        // 再生速度(倍率).
        public float speedRate = 1f;

        // Pause前の再生速度.
        private float pausedSpeed = 0f;

        [NonSerialized]
        private bool isInitialized = false;

        //----- property -----

        public bool IsPlaying
        {
            get { return State != State.Stop; }
        }

        public State State
        {
            get
            {
                return isPause ? State.Pause : currentState;
            }
        }

        public EndActionType EndActionType
        {
            get { return endActionType; }

            set { endActionType = value; }
        }

        public bool Pause
        {
            get { return isPause; }

            set
            {
                if (!isInitialized) { return; }

                SetPauseState(value);
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

        private void Initialize()
        {
            var animator = UnityUtility.GetOrAddComponent<Animator>(gameObject);

            if (animator == null) { return; }

            if (isInitialized) { return; }

            animator.runtimeAnimatorController = animatorController;
            animatorInfo = new AnimatorInfo(animator);

            Stop();

            Refresh();

            isInitialized = true;
        }

        void OnEnable()
        {
            Initialize();
        }

        void OnDisable()
        {
            Stop();
        }

        public IObservable<Unit> Play(string animationName)
        {
            if (string.IsNullOrEmpty(animationName)) { return Observable.ReturnUnit(); }

            UnityUtility.SetActive(gameObject, true);

            // ヒエラルキー上で非アクティブでないか.
            if (!UnityUtility.IsActiveInHierarchy(gameObject))
            {
                Debug.LogErrorFormat("Animation can't play not active in hierarchy.\n{0}", gameObject.transform.name);
                return Observable.ReturnUnit();
            }

            this.animationName = animationName;

            var hash = Animator.StringToHash(animationName);

            if (animatorInfo.Animator.HasState(0, hash))
            {
                return Observable.FromCoroutine(() => PlayInternal());
            }

            this.animationName = string.Empty;

            Debug.LogErrorFormat("Animation State Not found. {0}", animationName);

            return Observable.ReturnUnit();
        }

        private IEnumerator PlayInternal()
        {
            var hash = Animator.StringToHash(animationName);

            while (true)
            {
                if (!UnityUtility.IsActiveInHierarchy(gameObject)) { break; }

                // リセット.
                Refresh();

                // 再生速度設定.
                ApplySpeedRate();

                // 再生.
                currentState = State.Play;
                animatorInfo.Animator.Play(hash, -1, 0f);

                // 指定アニメーションへ遷移待ち.
                yield return Observable.FromCoroutine(() => WaitTransitionState()).ToYieldInstruction();

                // アニメーションの終了待ち.
                yield return Observable.FromCoroutine(() => WaitForEndOfAnimation()).ToYieldInstruction();

                if (endActionType != EndActionType.Loop) { break; }

                if (State == State.Stop) { yield break; }
            }

            EndAction();
        }

        // 指定アニメーションへ遷移待ち.
        private IEnumerator WaitTransitionState()
        {
            while (true)
            {
                if (UnityUtility.IsNull(gameObject)) { yield break; }

                if (!UnityUtility.IsActiveInHierarchy(gameObject)) { break; }

                var stateInfo = animatorInfo.Animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(animationName)) { break; }

                yield return null;
            }
        }

        // アニメーションの終了待ち.
        private IEnumerator WaitForEndOfAnimation()
        {
            if (!isInitialized) { yield break; }

            waitStateTransition = true;

            while (true)
            {
                if (State == State.Stop) { yield break; }

                if (State == State.Play)
                {
                    // 終了監視.
                    if (!IsAlive()) { break; }
                }

                yield return null;
            }
        }

        public void Stop()
        {
            if (!isInitialized) { return; }

            if (State == State.Stop) { return; }

            if (UnityUtility.IsNull(animatorInfo.Animator)) { return; }

            // Animator停止.
            foreach (var clip in animatorInfo.Clips)
            {
                clip.SampleAnimation(animatorInfo.Animator.gameObject, clip.length);
            }

            Refresh();
        }

        private void ApplySpeedRate()
        {
            animatorInfo.Animator.speed *= speedRate;
        }

        private void Refresh()
        {
            if (!isInitialized) { return; }

            currentState = State.Stop;

            // TimeScaleの影響を受けるか.
            animatorInfo.Animator.updateMode = ignoreTimeScale ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;

            if (UnityUtility.IsActiveInHierarchy(gameObject))
            {
                // 実行中にSampleAnimationしても表示された物が更新されない為、一旦再生して表示物をリセットする.
                var currentStateInfo = animatorInfo.Animator.GetCurrentAnimatorStateInfo(0);

                animatorInfo.Animator.Play(currentStateInfo.fullPathHash, -1, 0.0f);
            }

            animatorInfo.Animator.speed = animatorInfo.DefaultSpeed;
        }

        private void EndAction()
        {
            switch (endActionType)
            {
                case EndActionType.None:
                    currentState = State.Stop;
                    break;

                case EndActionType.Deactivate:
                    if (Application.isPlaying)
                    {
                        UnityUtility.SetActive(gameObject, false);
                    }

                    currentState = State.Stop;
                    break;

                case EndActionType.Destroy:
                    if (Application.isPlaying)
                    {
                        UnityUtility.SafeDelete(gameObject);
                    }

                    currentState = State.Stop;
                    break;

                case EndActionType.Loop:
                    break;
            }

            if (onEndAnimation != null)
            {
                onEndAnimation.OnNext(this);
            }
        }

        public IObservable<AnimationPlayer> EndAnimationAsObservable()
        {
            return onEndAnimation ?? (onEndAnimation = new Subject<AnimationPlayer>());
        }

        /// <summary>
        /// 配下のAnimatorに対してパラメータをセット.
        /// </summary>
        /// <param name="parameters"></param>
        public void SetParameters(params StateMachineParameter[] parameters)
        {
            DoLazyFunc(animator =>
            {
                foreach (var parameter in parameters)
                {
                    parameter.SetParameter(animator);
                }
                return Unit.Default;
            })
            .Subscribe()
            .AddTo(this);
        }

        /// <summary>
        /// 指定した型の<see cref="StateMachineBehaviour"/>をすべて取得.
        /// <see cref="Animator"/>の初期化が完了するまで取得出来ないため、非同期で取得.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IObservable<T[]> GetStateMachineBehavioursAsync<T>() where T : StateMachineBehaviour
        {
            return DoLazyFunc(x => x.GetBehaviours<T>())
                .SelectMany(x => x)
                .ToArray();
        }

        private void SetPauseState(bool pause)
        {
            if (!isPause && pause)
            {
                pausedSpeed = animatorInfo.Animator.speed;
            }

            animatorInfo.Animator.speed = pause ? 0f : pausedSpeed;
        }

        private void ResetAnimatorSpeed()
        {
            animatorInfo.Animator.speed = animatorInfo.DefaultSpeed;
        }

        private bool IsAlive()
        {
            if (!UnityUtility.IsActiveInHierarchy(gameObject)) { return false; }

            var stateInfo = animatorInfo.Animator.GetCurrentAnimatorStateInfo(0);

            // 指定ステートへ遷移待ち.
            if (waitStateTransition)
            {
                if (!stateInfo.IsName(animationName) || animatorInfo.Animator.IsInTransition(0))
                {
                    return true;
                }

                waitStateTransition = false;
            }

            if (!stateInfo.IsName(animationName)) { return false; }

            if (1 < stateInfo.normalizedTime && !animatorInfo.Animator.IsInTransition(0)) { return false; }

            return true;
        }

        private IObservable<TResult> DoLazyFunc<TResult>(Func<Animator, TResult> func)
        {
            // 初期化済の場合は即座に実行.
            if (animatorInfo.Animator.isInitialized)
            {
                return Observable.Return(func(animatorInfo.Animator));
            }

            // 初期化済でない場合は初期化完了を待つ.
            // これは非アクティブでも回って欲しいのであえて Observable.EveryUpdateを使う.
            return Observable.EveryUpdate()
                .SkipWhile(_ => !animatorInfo.Animator.isInitialized)
                .Take(1)
                .Select(_ => func(animatorInfo.Animator));
        }

        #region StateMachine Event

        public void StateMachineEvent(StateMachineEvent stateMachineEvent)
        {
            if (onStateMachineEvent != null)
            {
                onStateMachineEvent.OnNext(stateMachineEvent);
            }
        }

        public IObservable<StateMachineEvent> StateEventAsObservable()
        {
            return onStateMachineEvent ?? (onStateMachineEvent = new Subject<StateMachineEvent>());
        }

        #endregion

        #region Animation Event

        public void Event(string value)
        {
            if (onAnimationEvent != null)
            {
                onAnimationEvent.OnNext(value);
            }
        }

        public IObservable<string> OnAnimationEventAsObservable()
        {
            return onAnimationEvent ?? (onAnimationEvent = new Subject<string>());
        }

        #endregion        
    }
}