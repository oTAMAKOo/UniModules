
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CustomEditor(typeof(UIComponentBehaviour), true)]
    public sealed class UIComponentInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            DrawDefaultScriptlessInspector();
        }
    }
}
