﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.LogHandler;

namespace Modules.InputProtection
{
    public sealed class InputProtect : Extensions.Scope
    {
        //----- params -----

        //----- field -----

        private static int EntityId = 0;

        //----- property -----

        public int Id { get; private set; }

        //----- method -----

        public InputProtect()
        {
            Id = EntityId++;

            InputProtectManager.Instance.Lock(Id);
        }

        protected override void CloseScope()
        {
            InputProtectManager.Instance.Unlock(Id);
        }
    }

    /// <summary>
    /// 入力後の処理実行中に他の入力が実行されてしまわないようにするクラス.
    /// </summary>
    public sealed class InputProtectManager : Singleton<InputProtectManager>
	{
        //----- params -----

        //----- field -----

        private HashSet<int> entityIds = new HashSet<int>();
        private Subject<bool> onUpdateProtect = null;

        //----- property -----

        public bool IsProtect { get { return entityIds.Any(); } }

        //----- method -----

        private InputProtectManager()
        {
            // Exception発生時に強制解除.
            ApplicationLogHandler.Instance.OnReceiveExceptionAsObservable()
                .Subscribe(x => ForceUnlock())
                .AddTo(Disposable);
        }

        public void Lock(int entityId)
        {
            var isProtected = IsProtect;

            entityIds.Add(entityId);

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        public void Unlock(int entityId)
        {
            if (!IsProtect) { return; }

            var isProtected = IsProtect;

            entityIds.Remove(entityId);

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        public void ForceUnlock()
        {
            if (!IsProtect) { return; }

            var isProtected = IsProtect;

            entityIds.Clear();

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        public IObservable<bool> OnUpdateProtectAsObservable()
        {
            return onUpdateProtect ?? (onUpdateProtect = new Subject<bool>());
        }
    }
}
