
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

        private Texture textureAsset = null;

        private Texture developmentTexture = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as UIRawImage;

            textureAsset = null;

            var assetGuid = Reflection.GetPrivateField<UIRawImage, string>(instance, "assetGuid");

            if (!string.IsNullOrEmpty(assetGuid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    textureAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;
                }
            }

            if (instance.texture != null && instance.texture.name == UIRawImage.DevelopmentAssetName)
            {
                developmentTexture = instance.texture;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIRawImage;

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Development Texture");

                EditorGUI.BeginChangeCheck();

                textureAsset = EditorGUILayout.ObjectField(textureAsset, typeof(Texture), false) as Texture;

                if (EditorGUI.EndChangeCheck())
                {
                    var assetGuid = string.Empty;

                    if (textureAsset != null)
                    {
                        assetGuid = UnityEditorUtility.GetAssetGUID(textureAsset);
                    }

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    Reflection.InvokePrivateMethod(instance, "ApplyDevelopmentAsset");
                }
            }

            if (instance.texture != null && instance.texture.name == UIRawImage.DevelopmentAssetName)
            {
                developmentTexture = instance.texture;
            }
            else
            {
                UnityUtility.SafeDelete(developmentTexture);
            }
        }
    }
}
