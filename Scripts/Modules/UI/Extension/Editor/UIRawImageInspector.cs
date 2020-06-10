
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CustomEditor(typeof(UIRawImage), true)]
    public sealed class UIRawImageInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Texture developmentTexture = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as UIRawImage;

            var assetGuid = Reflection.GetPrivateField<UIRawImage, string>(instance, "assetGuid");

            if (!string.IsNullOrEmpty(assetGuid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                developmentTexture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIRawImage;

            DrawDefaultScriptlessInspector();

            EditorGUI.BeginChangeCheck();

            developmentTexture = EditorGUILayout.ObjectField("Development Texture", developmentTexture, typeof(Object), false) as Texture;

            if (EditorGUI.EndChangeCheck())
            {
                SetAssetGuid(instance, developmentTexture);

                Reflection.InvokePrivateMethod(instance, "ApplyDevelopmentAsset");
            }
        }

        private void SetAssetGuid(UIRawImage instance, UnityEngine.Object asset)
        {
            var assetGuid = string.Empty;

            if (asset != null)
            {
                assetGuid = UnityEditorUtility.GetAssetGUID(asset);
            }

            Reflection.SetPrivateField(instance, "assetGuid", assetGuid);            
        }
    }
}
