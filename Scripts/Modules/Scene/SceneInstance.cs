
using UnityEngine;
using System.Linq;
using Constants;
using Extensions;

namespace Modules.Scene
{
    /// <summary> シーン情報 </summary>
    public sealed class SceneInstance
    {
        //----- params -----

        //----- field -----

        private UnityEngine.SceneManagement.Scene? scene = null;

		private GameObject[] activeRoots = null;

        //----- property -----

        public Scenes? Identifier { get; private set; }

		public bool IsEnable { get; private set; }

        public ISceneBase Instance { get; private set; }

        //----- method -----

        public SceneInstance(Scenes? identifier, ISceneBase instance, UnityEngine.SceneManagement.Scene? scene)
        {
            this.scene = scene;
            
            Identifier = identifier;
            Instance = instance;
			IsEnable = true;

            var rootObjects = scene.Value.GetRootGameObjects();

            activeRoots = rootObjects
                .Where(x => !UnityUtility.IsNull(x))
                .Where(x => UnityUtility.IsActive(x))
                .ToArray();
        }

        public bool Enable()
        {
            if (!scene.HasValue) { return false; }

            if (IsEnable) { return true; }

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

			IsEnable = true;

            return true;
        }

        public bool Disable()
        {
            if (!scene.HasValue) { return false; }

            if (!IsEnable) { return true; }

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

			IsEnable = false;

            return true;
        }

        public UnityEngine.SceneManagement.Scene? GetScene()
        {
            return scene;
        }
    }
}
