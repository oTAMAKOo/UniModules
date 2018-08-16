
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.EventHook
{
    public class ComponentTuningConfig : ReloadableScriptableObject<ComponentTuningConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private ComponentTuner[] componentTuners = null;

        //----- property -----

	    public ComponentTuner[] ComponentTuners { get { return componentTuners; } }

        //----- method -----
    }
}