
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using Constants;
using UniRx;
using Extensions;

namespace Modules.Scene
{
    /// <summary> シーン情報 </summary>
    public sealed class SceneInstance
    {
        //----- params -----

        //----- field -----

        private UnityEngine.SceneManagement.Scene? scene = null;
        private bool enable = true;
        private GameObject[] activeRoots = null;

        //----- property -----

        public Scenes? Identifier { get; private set; }
        public ISceneBase Instance { get; private set; }

        //----- method -----

        public SceneInstance(Scenes? identifier, ISceneBase instance, UnityEngine.SceneManagement.Scene? scene)
        {
            this.scene = scene;
            
            Identifier = identifier;
            Instance = instance;

            var rootObjects = scene.Value.GetRootGameObjects();

            activeRoots = rootObjects
                .Where(x => !UnityUtility.IsNull(x))
                .Where(x => UnityUtility.IsActive(x))
                .ToArray();
        }

        public bool Enable()
        {
            if (!scene.HasValue) { return false; }

            if (enable) { return true; }

            if (activeRoots == null) { return true; }

            if (!scene.Value.isLoaded || !scene.Value.IsValid()) { return false; }

            var rootObjects = scene.Value.GetRootGameObjects();

            foreach (var rootObject in activeRoots)
            {
                if (UnityUtility.IsNull(rootObject)) { continue; }

                if (!rootObjects.Contains(rootObject)) { continue; }

                var ignoreControl = UnityUtility.GetComponent<IgnoreControl>(rootObject);

                if (ignoreControl != null)
                {
                    if (ignoreControl.Type.HasFlag(IgnoreControl.IgnoreType.ActiveControl)) { continue; }
                }

                UnityUtility.SetActive(rootObject, true);
            }

            enable = true;

            return true;
        }

        public bool Disable()
        {
            if (!scene.HasValue) { return false; }

            if (!enable) { return true; }

            if (!scene.Value.isLoaded || !scene.Value.IsValid()) { return false; }

            var rootObjects = scene.Value.GetRootGameObjects();

            foreach (var rootObject in activeRoots)
            {
                if (!rootObjects.Contains(rootObject)){ continue; }
                
                var ignoreControl = UnityUtility.GetComponent<IgnoreControl>(rootObject);

                if (ignoreControl != null)
                {
                    if (ignoreControl.Type.HasFlag(IgnoreControl.IgnoreType.ActiveControl)) { continue; }
                }

                UnityUtility.SetActive(rootObject, false);
            }

            enable = false;

            return true;
        }

        public UnityEngine.SceneManagement.Scene? GetScene()
        {
            return scene;
        }
    }
}
