
using UnityEngine;
using UniRx;

namespace Modules.BackKey
{
    public abstract class BackKeyReceiver : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private int priority = 0;

        private bool initialized = false;

        //----- property -----

        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        //----- method -----

        void OnEnable()
        {
            var backKeyManager = BackKeyManager.Instance;

            Initialize();

            backKeyManager.AddReceiver(this);
        }

        void OnDisable()
        {
            var backKeyManager = BackKeyManager.Instance;

            backKeyManager.RemoveReceiver(this);
        }

        private void Initialize()
        {
            if (initialized){ return; }

            OnInitialize();

            initialized = true;
        }

        protected virtual void OnInitialize() { }

        public abstract bool HandleBackKey();
    }
}