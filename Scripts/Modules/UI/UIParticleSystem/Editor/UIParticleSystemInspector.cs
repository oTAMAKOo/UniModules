
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI.Particle
{
    [CustomEditor(typeof(UIParticleSystem))]
    public sealed class UIParticleSystemInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as UIParticleSystem;

            EditorGUI.BeginChangeCheck();

            var useLengthScale = EditorGUILayout.Toggle("Use UseLengthScale", instance.UseLengthScale);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                instance.UseLengthScale = useLengthScale;
            }

            EditorGUI.BeginChangeCheck();

            var use3dRotation = EditorGUILayout.Toggle("Use Use3dRotation", instance.Use3dRotation);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                instance.Use3dRotation = use3dRotation;
            }

            EditorGUI.BeginChangeCheck();

            var useOverrideMaterial = EditorGUILayout.Toggle("Use Override Material", instance.UseOverrideMaterial);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                instance.UseOverrideMaterial = useOverrideMaterial;
            }
        }
    }
}


