
using UnityEngine;
using Extensions;
using UniRx;

namespace Modules.TimeUtil
{
    /// <summary>
    /// TimeScaleに影響されない現実時間クラス.
    /// </summary>
    public sealed class RealTime : Singleton<RealTime>
    {
        //----- params -----


        //----- field -----

        private float realTime = 0f;
        private float realDelta = 0f;

        //----- property -----

        public static float time
        {
            get
            {
                return Application.isPlaying ? Instance.realTime : realtimeSinceStartup;
            }
        }

        public static float deltaTime
        {
            get
            {
                return Application.isPlaying ? Instance.realDelta : 0f;
            }
        }

        private static float realtimeSinceStartup
        {
            get { return UnityEngine.Time.realtimeSinceStartup; }
        }

        //----- method -----

        protected override void OnCreate()
        {
            realTime = realtimeSinceStartup;

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateTime())
                .AddTo(Disposable);
        }
        
        private void UpdateTime()
        {
            realDelta = Mathf.Clamp01(realtimeSinceStartup - realTime);

            realTime = realtimeSinceStartup;
        }
    }
}
