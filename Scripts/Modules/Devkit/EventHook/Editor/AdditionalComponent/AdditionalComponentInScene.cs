
using UnityEngine;
using UnityEditor;
using UniRx;
using Extensions.Devkit;

namespace Modules.Devkit.EventHook
{
    public sealed class AdditionalComponentInScene : AdditionalComponent
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            HierarchyChangeNotification.OnCreatedAsObservable().Subscribe(x => AddRequireComponents(x, CheckExecute));
        }

        private static bool CheckExecute(GameObject target)
        {
            var isPrefab = UnityEditorUtility.IsPrefab(target);

            // Prefabは処理しない.
            if (isPrefab) { return false; }

            return true;
        }
    }
}
