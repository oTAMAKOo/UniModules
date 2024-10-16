
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.DummyContent
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(DummySprite), true)]
    public sealed class DummySpriteInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

		private Sprite spriteAsset = null;

		private Sprite dummySprite = null;

        //----- property -----

        //----- method -----

		void OnEnable()
        {
            var instance = target as DummySprite;

            spriteAsset = null;

            var assetGuid = Reflection.GetPrivateField<DummySprite, string>(instance, "assetGuid");

            var spriteId = Reflection.GetPrivateField<DummySprite, string>(instance, "spriteId");

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

            if (image.sprite != null && image.sprite.name == DummySprite.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as DummySprite;

            var image = instance.Image;

            if (image == null) { return; }

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("DummySprite");

                EditorGUI.BeginChangeCheck();

                spriteAsset = EditorGUILayout.ObjectField(spriteAsset, typeof(Sprite), false) as Sprite;

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    var assetGuid = string.Empty;
                    var spriteId = string.Empty;

                    if (spriteAsset != null)
                    {
                        var asset = AssetDatabase.IsMainAsset(spriteAsset.texture) ? 
                                    (Object)spriteAsset.texture : 
                                    (Object)spriteAsset;
                       
                        var assetPath = AssetDatabase.GetAssetPath(asset);
                    
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            assetGuid =  AssetDatabase.AssetPathToGUID(assetPath);
                        }

                        spriteId = spriteAsset.GetSpriteID().ToString();
                    }

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    Reflection.SetPrivateField(instance, "spriteId", spriteId);

                    if (spriteAsset == null)
                    {
                        if (image.sprite != null && image.sprite.name == DummySprite.DummyAssetName)
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

            if (image.sprite != null && image.sprite.name == DummySprite.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }
    }
}