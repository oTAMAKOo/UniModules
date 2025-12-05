
#if ENABLE_CRIWARE_SOFDEC

using System;
using UniRx;
using CriWare;
using CriWare.CriMana;
using Extensions;

namespace Modules.Movie
{
    public sealed class MovieElement : LifetimeDisposable
    {
        //----- params -----

        public enum Status
        {
            None = 0,

            Play,
            Pause,
            Stop,
        }

        //----- field -----

        private IMovieManagement movieManagement = null;
        
        private Subject<Unit> onFinish = null;

        //----- property -----

        public CriManaMovieMaterialBase CriManaMovieMaterial { get; private set; }

        /// <summary> 動画ファイルパス. </summary>
        public string MoviePath { get; private set; }

        /// <summary> 動画プレイヤー. </summary>
        public Player Player { get; private set; }

        /// <summary> 動画状態. </summary>
        public Status ElementStatus { get; private set; }

        /// <summary> 動画状態. </summary>
        public Player.Status? PlayerStatus { get; private set; }

        /// <summary> 再生準備が完了しているか. </summary>
        public bool IsReady { get { return PlayerStatus == Player.Status.Ready; } }

        /// <summary> エラーが発生しているか. </summary>
        public bool IsError { get { return PlayerStatus == Player.Status.Error; } }

        /// <summary> 終了済みか. </summary>
        public bool IsFinished { get; private set; }

        /// <summary> ループ再生. </summary>
        public bool IsLoop { get; private set; }

        /// <summary>
        /// 再生時間(秒).
        /// 再生準備中などで情報が取得できない時は-1.
        /// </summary>
        public float PlayTime { get; private set; }

        /// <summary>
        /// 動画時間(秒).
        /// 再生準備中などで情報が取得できない時は-1.
        /// </summary>
        public float TotalTime { get; private set; }

        //----- method -----

        public MovieElement(IMovieManagement movieManagement, Player moviePlayer, CriManaMovieMaterialBase criManaMovieMaterial, string moviePath)
        {
            this.movieManagement = movieManagement;

            CriManaMovieMaterial = criManaMovieMaterial;
            MoviePath = moviePath;
            Player = moviePlayer;
            PlayerStatus = moviePlayer.status;
            ElementStatus = Status.None;
            IsFinished = false;

            OnFinishAsObservable()
                .Subscribe(_ => ReleasePlayer())
                .AddTo(Disposable);
        }

        public void Play(bool loop = false)
        {
            IsLoop = loop;

            movieManagement.Play(this, loop);
        }

        public void Pause(bool pause)
        {
            movieManagement.Pause(this, pause);
        }

        public void Stop()
        {
            movieManagement.Stop(this);
        }

        public void SetStatus(Status status)
        {
            ElementStatus = status;
        }

        public void Update() 
        {
            if (UnityUtility.IsNull(CriManaMovieMaterial)) { return; }

            var prevStatus = PlayerStatus;

            PlayerStatus = Player.status;

            PlayTime = GetPlayTime();
            TotalTime = GetTotalTime();

            var isFinish = false;

            if (ElementStatus == Status.Play)
            {
                if (prevStatus != PlayerStatus && PlayerStatus == Player.Status.PlayEnd)
                {
                    if (IsLoop)
                    {
                        Player.Start();
                    }
                    else
                    {
                        isFinish = true;
                    }
                }
            }

            isFinish |= !Player.isAlive;
            isFinish |= ElementStatus == Status.Stop;
            isFinish |= PlayerStatus == Player.Status.Error;
            
            if(isFinish)
            {
                Finish();
            }
        }

        private void Finish()
        {
            UnityUtility.SetActive(CriManaMovieMaterial, false);

            IsFinished = true;

            if (onFinish != null)
            {
                onFinish.OnNext(Unit.Default);
            }
        }

        private float GetPlayTime()
        {
            var frameInfo = Player.frameInfo;

            if (frameInfo == null) { return -1f; }

            return (float)frameInfo.frameNo / frameInfo.framerateN / frameInfo.framerateD * 1000000.0f;
        }

        private float GetTotalTime()
        {
            var movieInfo = Player.movieInfo;

            if (movieInfo == null) { return -1f; }
            
            return movieInfo.totalFrames * 1000.0f / movieInfo.framerateN;
        }

        private void ReleasePlayer()
        {
            if (Player != null)
            {
                Player = null;
            }

            if (CriManaMovieMaterial != null)
            {
                CriManaMovieMaterial.PlayerManualFinalize();

                CriManaMovieMaterial = null;
            }
        }

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif
