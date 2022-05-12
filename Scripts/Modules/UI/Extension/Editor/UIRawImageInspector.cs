
using Extensions;
using UnityEditor;
using UnityEngine;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIRawImage), true)]
    public sealed class UIRawImageInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Texture textureAsset = null;

        private Texture dummyTexture = null;

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

            var rawImage = instance.RawImage;

            if (rawImage != null)
            {
                if (rawImage.texture != null && rawImage.texture.name == UIRawImage.DummyAssetName)
                {
                    dummyTexture = rawImage.texture;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIRawImage;

            var rawImage = instance.RawImage;

            if (rawImage == null) { return; }

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Dummy Texture");

                EditorGUI.BeginChangeCheck();

                textureAsset = EditorGUILayout.ObjectField(textureAsset, typeof(Texture), false) as Texture;

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    var assetGuid = textureAsset != null ? UnityEditorUtility.GetAssetGUID(textureAsset) : string.Empty;

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    if (textureAsset == null)
                    {
                        if (rawImage.texture != null && rawImage.texture.name == UIRawImage.DummyAssetName)
                        {
                            rawImage.texture = null;
                        }
                    }
                    else
                    {
                        Reflection.InvokePrivateMethod(instance, "ApplyDummyAsset");
                    }
                }
            }

            if (dummyTexture != null)
            {
                if (rawImage.texture != dummyTexture)
                {
                    UnityUtility.SafeDelete(dummyTexture);

                    dummyTexture = null;
                }
            }

            if (rawImage.texture != null && rawImage.texture.name == UIRawImage.DummyAssetName)
            {
                dummyTexture = rawImage.texture;
            }
        }
    }
}
