
using UnityEngine;
using Extensions;
using UniRx;

namespace Modules.TimeManagement
{
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
                return Application.isPlaying ? Instance.realTime : Time.realtimeSinceStartup;
            }
        }

        public static float deltaTime
        {
            get
            {
                return Application.isPlaying ? Instance.realDelta : 0f;
            }
        }

        //----- method -----

        protected override void OnCreate()
        {
            realTime = Time.realtimeSinceStartup;

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateTime())
                .AddTo(Disposable);
        }
        
        private void UpdateTime()
        {
            var rt = Time.realtimeSinceStartup;

            realDelta = Mathf.Clamp01(rt - realTime);

            realTime = rt;
        }
    }
}
