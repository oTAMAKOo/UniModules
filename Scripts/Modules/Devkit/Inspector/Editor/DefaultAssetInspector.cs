
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Modules.Devkit.Inspector
{
    public abstract class ExtendInspector
    {
        public abstract int Priority { get; }

        public abstract bool Validation(UnityEngine.Object target);

        public abstract void DrawInspectorGUI(UnityEngine.Object target);

        public virtual void OnEnable(UnityEngine.Object target) { }

        public virtual void OnDisable(UnityEngine.Object target) { }

        public virtual void OnDestroy(UnityEngine.Object target) { }
    }

    [CustomEditor(typeof(DefaultAsset))]
    public sealed class DefaultAssetInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private static HashSet<Type> extendInspectorTypes = new HashSet<Type>();

        private ExtendInspector[] extendInspectors = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            CreateInspectorClass();

            foreach (var extendInspector in extendInspectors)
            {
                extendInspector.OnEnable(target);
            }
        }

        void OnDisable()
        {
            foreach (var extendInspector in extendInspectors)
            {
                extendInspector.OnDisable(target);
            }
        }

        void OnDestroy()
        {
            foreach (var extendInspector in extendInspectors)
            {
                extendInspector.OnDestroy(target);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foreach (var extendInspector in extendInspectors)
            {
                GUI.enabled = true;

                extendInspector.DrawInspectorGUI(target);

                GUI.enabled = false;
            }
        }

        private void CreateInspectorClass()
        {
            extendInspectors = extendInspectorTypes
                .Select(x => Activator.CreateInstance(x))                
                .Cast<ExtendInspector>()
                .Where(x => x.Validation(target))
                .OrderBy(x => x.Priority)
                .ToArray();
        }

        public static void AddExtendInspector<T>() where T : ExtendInspector
        {
            extendInspectorTypes.Add(typeof(T));
        }
    }
}
