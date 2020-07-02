
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.DummyAssetSetter
{
    [CustomEditor(typeof(DummySpriteSetter))]
    public sealed class DummySpriteSetterInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Sprite spriteAsset = null;

        private Sprite dummySprite = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as DummySpriteSetter;

            spriteAsset = null;

            var assetGuid = Reflection.GetPrivateField<DummySpriteSetter, string>(instance, "assetGuid");

            var spriteId = Reflection.GetPrivateField<DummySpriteSetter, string>(instance, "spriteId");

            if (!string.IsNullOrEmpty(assetGuid) && !string.IsNullOrEmpty(spriteId))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (!string.IsNullOrEmpty(assetPath))
                {
                    spriteAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                        .OfType<Sprite>()
                        .FirstOrDefault(x => x.GetSpriteID().ToString() == spriteId);
                }
            }

            var image = instance.Image;

            if (image.sprite != null && image.sprite.name == DummySpriteSetter.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as DummySpriteSetter;

            var image = instance.Image;

            if (image == null){ return; }

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Dummy Sprite");

                EditorGUI.BeginChangeCheck();

                spriteAsset = EditorGUILayout.ObjectField(spriteAsset, typeof(Sprite), false) as Sprite;

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("DummySpriteSetterInspector-Undo", instance);

                    var assetGuid = spriteAsset != null ? UnityEditorUtility.GetAssetGUID(spriteAsset.texture) : string.Empty;

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    var spriteId = spriteAsset != null ? spriteAsset.GetSpriteID().ToString() : string.Empty;

                    Reflection.SetPrivateField(instance, "spriteId", spriteId);

                    if (spriteAsset == null)
                    {
                        if (image.sprite != null && image.sprite.name == DummySpriteSetter.DummyAssetName)
                        {
                            image.sprite = null;
                        }
                    }
                    else
                    {
                        Reflection.InvokePrivateMethod(instance, "ApplyDummyAsset");
                    }
                }
            }

            if (dummySprite != null)
            {
                if (image.sprite != dummySprite)
                {
                    UnityUtility.SafeDelete(dummySprite);

                    dummySprite = null;
                }
            }

            if (image.sprite != null && image.sprite.name == DummySpriteSetter.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }
    }
}
