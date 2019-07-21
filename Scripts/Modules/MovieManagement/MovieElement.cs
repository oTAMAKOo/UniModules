
#if ENABLE_CRIWARE_SOFDEC

using System;
using UniRx;
using CriMana;
using Extensions;

namespace Modules.MovieManagement
{
    public sealed class MovieElement
	{
        //----- params -----

        //----- field -----

        private Player manaPlayer = null;
        private CriManaMovieControllerForUI movieController = null;
        private Subject<Unit> onFinish = null;

        //----- property -----

        public string MoviePath { get; private set; }
        public Player.Status? Status { get; private set; }

        //----- method -----

        public MovieElement(Player manaPlayer, CriManaMovieControllerForUI movieController, string moviePath)
        {
            this.manaPlayer = manaPlayer;
            this.movieController = movieController;

            MoviePath = moviePath;
            Status = manaPlayer.status;
        }

        public void Update()
        {
            if (onFinish != null)
            {
                if (Status != manaPlayer.status && manaPlayer.status == Player.Status.PlayEnd)
                {
                    onFinish.OnNext(Unit.Default);
                }
            }

            if (UnityUtility.IsNull(movieController))
            {
                Status = Player.Status.PlayEnd;
            }
            else
            {
                Status = manaPlayer.status;
            }            
        }
        
        public Player GetPlayer()
        {
            return manaPlayer;
        }

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif
