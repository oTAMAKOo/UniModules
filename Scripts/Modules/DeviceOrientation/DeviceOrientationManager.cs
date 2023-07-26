
using System;
using UnityEngine;
using UniRx;
using Extensions;
using Modules.ApplicationEvent;

namespace Modules.DeviceOrientation
{
    public abstract class DeviceOrientationManager : Singleton<DeviceOrientationManager>
    {
        //----- params -----

        //----- field -----

        private Subject<ScreenOrientation> onOrientationChanged = null;

        //----- property -----

        public ScreenOrientation Orientation { get { return Screen.orientation; } }

        //----- method -----

        public void Initialize()
        {
            Apply();

            // 画面方向が変化した時の処理.
            this.ObserveEveryValueChanged(x => x.Orientation)
                .Subscribe(x => OnOrientationChanged(x))
                .AddTo(Disposable);

            // レジュームした際に再適用.
            ApplicationEventHandler.OnResumeAsObservable()
                .Subscribe(_ => Apply())
                .AddTo(Disposable);
        }

        private void OnOrientationChanged(ScreenOrientation orientation)
        {
            if (orientation != ScreenOrientation.AutoRotation)
            {
                Apply(orientation);

                if (onOrientationChanged != null)
                {
                    onOrientationChanged.OnNext(orientation);
                }
            }
        }

        public IObservable<ScreenOrientation> OnOrientationChangedAsObservable()
        {
            return onOrientationChanged ?? (onOrientationChanged = new Subject<ScreenOrientation>());
        }

        /// <summary> 画面の向きを設定 </summary>

        /*------ 横持ち時の設定 ------
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        ------------------------------*/

        public abstract void Apply(ScreenOrientation? orientation = null);
    }
}