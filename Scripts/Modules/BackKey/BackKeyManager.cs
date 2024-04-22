
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;

namespace Modules.BackKey
{
    public sealed class BackKeyManager : Singleton<BackKeyManager>
    {
        //----- params -----

        //----- field -----

        private List<BackKeyReceiver> receivers = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            receivers = new List<BackKeyReceiver>();

            Observable.EveryLateUpdate()
                .Subscribe(_ => HandleBackKey())
                .AddTo(Disposable);
        }

        private void HandleBackKey()
        {
            #if UNITY_ANDROID
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                InvokeReceivers();
            }

            #endif
        }

        private void InvokeReceivers()
        {
            if (receivers.IsEmpty()){ return; }

            foreach (var receiver in receivers)
            {
                var result = receiver.HandleBackKey();

                if (result) { break; }
            }
        }

        public void AddReceiver(BackKeyReceiver receiver)
        {
            if (receivers.Contains(receiver)){ return; }

            receivers.Add(receiver);

            receivers.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        public void RemoveReceiver(BackKeyReceiver receiver)
        {
            if (!receivers.Contains(receiver)){ return; }

            receivers.Remove(receiver);

            receivers.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        public void ClearReceiver()
        {
            receivers.Clear();
        }
    }
}