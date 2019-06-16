
#if ENABLE_CRIWARE_SOFDEC

using UnityEditor;
using Modules.CriWare.Editor;

namespace Modules.MovieManagement.Editor
{
    [CustomEditor(typeof(MovieConfig))]
    public class MovieConfigInspector : CriAssetConfigInspectorBase
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDirectoryInspector(serializedObject);

            EditorGUILayout.Separator();
        }
    }
}

#endif
