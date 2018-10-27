
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextEffectBase))]
    public abstract class TextEffectBaseInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        protected bool update = false;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            EditorApplication.update += ApplyCallback;
        }

        void OnDisable()
        {
            EditorApplication.update -= ApplyCallback;
        }

        private void ApplyCallback()
        {
            var instance = target as TextEffectBase;

            if (instance == null) { return; }

            if (!update) { return; }

            Reflection.InvokePrivateMethod(instance, "Apply");

            update = false;
        }
    }
}
