
#if ENABLE_CRIWARE_SOFDEC

using System;
using UniRx;
using CriWare;
using CriWare.CriMana;
using Extensions;

namespace Modules.Movie
{
    public sealed class MovieElement
    {
        //----- params -----

        //----- field -----
        
        private Subject<Unit> onFinish = null;

        //----- property -----

        public CriManaMovieMaterialBase CriManaMovieMaterial { get; private set; }

        /// <summary> 動画ファイルパス. </summary>
        public string MoviePath { get; private set; }

        /// <summary> 動画プレイヤー. </summary>
        public Player Player { get; private set; }

        /// <summary> 動画状態. </summary>
        public Player.Status? Status { get; private set; }

        /// <summary> 再生準備が完了しているか. </summary>
        public bool IsReady { get { return Status == Player.Status.Ready; } }

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

        public MovieElement(Player moviePlayer, CriManaMovieMaterialBase criManaMovieMaterial, string moviePath)
        {
            CriManaMovieMaterial = criManaMovieMaterial;
            MoviePath = moviePath;
            Player = moviePlayer;
            Status = moviePlayer.status;
            IsFinished = false;
        }

        public void Play(bool loop = false)
        {
            var movieManagement = MovieManagement.Instance;

            movieManagement.Play(this, loop);
        }

        public void Pause(bool pause)
        {
            var movieManagement = MovieManagement.Instance;

            movieManagement.Pause(this, pause);
        }

        public void Stop()
        {
            var movieManagement = MovieManagement.Instance;

            movieManagement.Stop(this);
        }

        public void Update()
        {
            if (UnityUtility.IsNull(CriManaMovieMaterial)) { return; }

            var prevStatus = Status;

            Status = Player.status;

            PlayTime = GetPlayTime();
            TotalTime = GetTotalTime();

            if (prevStatus != Status && Status == Player.Status.PlayEnd)
            {
                if (IsLoop)
                {
                    Player.Start();
                }
                else
                {
                    UnityUtility.SetActive(CriManaMovieMaterial.gameObject, false);

                    UnityUtility.DeleteComponent(CriManaMovieMaterial);

                    IsFinished = true;

                    if (onFinish != null)
                    {
                        onFinish.OnNext(Unit.Default);
                    }
                }
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

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif
