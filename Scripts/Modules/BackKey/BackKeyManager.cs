
using UnityEngine;
using System;
using UniRx;
using Extensions;

namespace Modules.BackKey
{
    public sealed class BackKeyManager : Singleton<BackKeyManager>
    {
        //----- params -----

        //----- field -----

        private Subject<Unit> onBackKey = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            Observable.EveryLateUpdate()
                .Subscribe(_ => HandleBackKey())
                .AddTo(Disposable);
        }

        private void HandleBackKey()
        {
            #if UNITY_ANDROID
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (onBackKey != null)
                {
                    onBackKey.OnNext(Unit.Default);
                }
            }

            #endif
        }

        public IObservable<Unit> OnBackKeyAsObservable()
        {
            return onBackKey ?? (onBackKey = new Subject<Unit>());
        }
    }
}