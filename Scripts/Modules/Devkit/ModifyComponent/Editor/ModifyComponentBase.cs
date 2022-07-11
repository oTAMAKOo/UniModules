
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Linq;
using System.Linq;
using Extensions.Devkit;

namespace Modules.Devkit.ModifyComponent
{
    public abstract class ModifyComponentBase<TComponent> where TComponent : Component
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public bool ModifyTargetComponent(string assetPath, TComponent target)
        {
            if (target == null) { return false; }

            if (PrefabUtility.IsPartOfPrefabInstance(target)) { return false; }

            var modified = Modify(assetPath, target);

            if (modified)
            {
                EditorUtility.SetDirty(target);
            }

            return modified;
        }

        public void UpdateAllSceneComponent()
        {
            using (new AssetEditingScope())
            {
                var sceneAssetPaths = AssetDatabase.FindAssets("t:scene")
                    .Select(x => AssetDatabase.GUIDToAssetPath(x))
                    .ToArray();

                var totalCount = sceneAssetPaths.Length;

                var title = string.Format("Update all scene component : {0}", typeof(TComponent).Name);

                for (var i = 0; i < totalCount; i++)
                {
                    var sceneAssetPath = sceneAssetPaths[i];

                    var scene = EditorSceneManager.OpenScene(sceneAssetPath);

                    EditorUtility.DisplayProgressBar(title, sceneAssetPath, (float)i / totalCount);

                    UpdateSceneComponent(scene);
                }

                EditorUtility.ClearProgressBar();
            }
        }

        public void UpdateSceneComponent(UnityEngine.SceneManagement.Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            var targets = rootObjects.SelectMany(x => x.DescendantsAndSelf().OfComponent<TComponent>()).ToArray();

            var updateCount = 0;

            foreach (var target in targets)
            {
                var hasUpdate = ModifyTargetComponent(scene.path, target);

                if (hasUpdate)
                {
                    updateCount++;
                }
            }

            if (0 < updateCount)
            {
                Debug.LogFormat("Update scene: {0}\ncount:{1}", scene.path, updateCount);

                EditorSceneManager.SaveScene(scene);
            }
        }

        public void UpdateAllPrefabComponent()
        {
            var prefabAssetPaths = AssetDatabase.FindAssets("t:prefab")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .ToArray();

            var totalCount = prefabAssetPaths.Length;

            var title = string.Format("Update all prefab component : {0}", typeof(TComponent).Name);

            for (var i = 0; i < totalCount; i++)
            {
                var prefabAssetPath = prefabAssetPaths[i];

                EditorUtility.DisplayProgressBar(title, prefabAssetPath, (float)i / totalCount);

                UpdatePrefabComponent(prefabAssetPath);
            }

            EditorUtility.ClearProgressBar();
        }

        public void UpdatePrefabComponent(string assetPath)
        {
            var go = PrefabUtility.LoadPrefabContents(assetPath);

            var targets = go.DescendantsAndSelf().OfComponent<TComponent>().ToArray();

            var updateCount = 0;

            foreach (var target in targets)
            {
                var hasUpdate = ModifyTargetComponent(assetPath, target);

                if (hasUpdate)
                {
                    updateCount++;
                }
            }

            if (0 < updateCount)
            {
                Debug.LogFormat("Update prefab: {0}\ncount:{1}", assetPath, updateCount);

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            }

            PrefabUtility.UnloadPrefabContents(go);
        }

        protected abstract bool Modify(string assetPath, TComponent target);
    }
}
