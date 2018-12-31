
using UnityEngine;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Devkit.EventHook
{
    public static class AdditionalComponentUtility
    {
        /// <summary>
        /// T1の上にT2のコンポーネントを移動する.
        /// </summary>
        public static void SetScriptOrder(GameObject gameObject, Component parent, Component target)
        {
            var components = new List<Component>(gameObject.GetComponents<Component>());

            var index = components.IndexOf(parent);
            var targetIndex = components.IndexOf(target);

            if (index < targetIndex)
            {
                for (var i = index; i < targetIndex; ++i)
                {
                    ComponentUtility.MoveComponentUp(target);
                }
            }
            else
            {
                for (var i = targetIndex; i < index; ++i)
                {
                    ComponentUtility.MoveComponentDown(target);
                }
            }
        }
    }
}
