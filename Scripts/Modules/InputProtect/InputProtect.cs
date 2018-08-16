﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Log;

namespace Modules.InputProtection
{
    /// <summary>
    /// 入力後の処理実行中に他の入力が実行されてしまわないようにするクラス.
    /// </summary>
    public static class InputProtect
	{
        //----- params -----

        public class Entity : Extensions.Scope
        {
            //----- params -----

            //----- field -----

            private static int EntityId = 0;

            //----- property -----

            public int Id { get; private set; }
            
            //----- method -----

            public Entity()
            {
                Id = EntityId++;

                Lock(Id);
            }

            protected override void CloseScope()
            {
                Unlock(Id);
            }
        }

        //----- field -----

        private static HashSet<int> entityIds = new HashSet<int>();
        private static Subject<bool> onUpdateProtect = null;

        //----- property -----

        public static bool IsProtect { get { return entityIds.Any(); } }

        //----- method -----

        static InputProtect()
        {
            // Exception発生時に強制解除.
            ApplicationLogHandler.Instance.OnReceiveExceptionAsObservable()
                .Subscribe(x => ForceUnlock());
        }

        private static void Lock(int entityId)
        {
            var isProtected = IsProtect;

            entityIds.Add(entityId);

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        private static void Unlock(int entityId)
        {
            if (!IsProtect) { return; }

            var isProtected = IsProtect;

            entityIds.Remove(entityId);

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        public static void ForceUnlock()
        {
            if (!IsProtect) { return; }

            var isProtected = IsProtect;

            entityIds.Clear();

            if (isProtected != IsProtect && onUpdateProtect != null)
            {
                onUpdateProtect.OnNext(IsProtect);
            }
        }

        public static IObservable<bool> OnUpdateProtectAsObservable()
        {
            return onUpdateProtect ?? (onUpdateProtect = new Subject<bool>());
        }
    }
}