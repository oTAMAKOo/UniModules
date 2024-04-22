
using UnityEngine;
using UniRx;

namespace Modules.BackKey
{
    public abstract class BackKeyReceiver : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private bool initialized = false;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var backKeyManager = BackKeyManager.Instance;

            void OnBackKey()
            {
                Initialize();
                HandleBackKey();
            }

            backKeyManager.OnBackKeyAsObservable()
                .TakeUntilDisable(this)
                .Subscribe(_ => OnBackKey())
                .AddTo(this);
        }

        private void Initialize()
        {
            if (initialized){ return; }

            OnInitialize();

            initialized = true;
        }

        protected virtual void OnInitialize() { }

        protected abstract void HandleBackKey();
    }
}