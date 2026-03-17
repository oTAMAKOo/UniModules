
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using R3;
using R3.Triggers;
using Extensions;

namespace Modules.Animation
{
    public sealed class ImmediateTransition : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Animator animator = null;

        //----- property -----

        //----- method -----

        void Awake()
        {
            animator = UnityUtility.GetComponent<Animator>(gameObject);
        }

        void OnEnable()
        {
            Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate)
                .FirstAsync()
                .TakeUntil(this.OnDisableAsObservable())
                .Subscribe(_ => ForceTransitionNextState())
                .AddTo(this);
        }

        private void ForceTransitionNextState()
        {
            // 「Entry」ステートにいる間は待機.
            while (true)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (!stateInfo.IsName("Entry")) { break; }

                animator.Update(0);
            }

            // 次のステートに遷移しただけでは内容が更新されてないのでもう1フレーム更新.
            animator.Update(0);
        }
    }
}
