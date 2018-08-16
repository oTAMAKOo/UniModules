
using UnityEngine;

namespace Modules.Devkit.SceneExecuter
{
    public class SceneExecuter : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string targetScenePath = null;

        //----- property -----

        public string TargetScenePath { get { return targetScenePath; } }

        //----- method -----
    }
}