
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.DummyAssetSetter
{
    [CustomEditor(typeof(DummyTextureSetter))]
    public sealed class DummyTextureSetterInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Texture textureAsset = null;

        private Texture dummyTexture = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as DummyTextureSetter;

            textureAsset = null;

            var assetGuid = Reflection.GetPrivateField<DummyTextureSetter, string>(instance, "assetGuid");

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
                if (rawImage.texture != null && rawImage.texture.name == DummyTextureSetter.DummyAssetName)
                {
                    dummyTexture = rawImage.texture;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as DummyTextureSetter;

            var rawImage = instance.RawImage;

            if (rawImage == null){ return; }

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Dummy Texture");

                EditorGUI.BeginChangeCheck();

                textureAsset = EditorGUILayout.ObjectField(textureAsset, typeof(Texture), false) as Texture;

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("DummyTextureSetterInspector-Undo", instance);

                    var assetGuid = textureAsset != null ? UnityEditorUtility.GetAssetGUID(textureAsset) : string.Empty;

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    if (textureAsset == null)
                    {
                        if (rawImage.texture != null && rawImage.texture.name == DummyTextureSetter.DummyAssetName)
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

            if (rawImage.texture != null && rawImage.texture.name == DummyTextureSetter.DummyAssetName)
            {
                dummyTexture = rawImage.texture;
            }
        }
    }
}
