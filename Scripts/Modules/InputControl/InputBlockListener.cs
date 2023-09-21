
using UnityEngine;
using UniRx;
using Extensions;

namespace Modules.InputControl
{
    public sealed class InputBlockListener : MonoBehaviour
    {
        //----- params -----
        
        //----- field -----

        private bool blocking = false;
        
        //----- property -----

        public bool IsBlocking { get { return blocking; } }

        //----- method -----

        void Start()
        {
            var blockInputManager = BlockInputManager.Instance;

            blockInputManager.OnUpdateStatusAsObservable()
                .Subscribe(x => UpdateInputBlock(x))
                .AddTo(this);

            UpdateInputBlock(blockInputManager.IsBlocking);
        }

        private void UpdateInputBlock(bool isBlock)
        {
            blocking = isBlock;

            UnityUtility.SetActive(gameObject, blocking);
        }
    }
}