
using System.Linq;
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

#if UNITY_2019_4_OR_NEWER
using UnityEditor.U2D;
#else
using UnityEditor.Experimental.U2D;
#endif

namespace Modules.UI.Extension
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIImage), true)]
    public sealed class UIImageInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Sprite spriteAsset = null;

        private Sprite dummySprite = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as UIImage;

            spriteAsset = null;

            var assetGuid = Reflection.GetPrivateField<UIImage, string>(instance, "assetGuid");

            var spriteId = Reflection.GetPrivateField<UIImage, string>(instance, "spriteId");

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

            if (image.sprite != null && image.sprite.name == UIImage.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIImage;

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

                    if (spriteAsset == null)
                    {
                        if (image.sprite != null && image.sprite.name == UIImage.DummyAssetName)
                        {
                            image.sprite = null;
                        }
                    }
                    else
                    {
                        var asset = AssetDatabase.IsMainAsset(spriteAsset.texture) ?
                                    (Object)spriteAsset.texture :
                                    (Object)spriteAsset;
                    
                        var assetPath = AssetDatabase.GetAssetPath(asset);
                    
                        var assetGuid =  AssetDatabase.AssetPathToGUID(assetPath);

                        Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                        var spriteId = spriteAsset.GetSpriteID().ToString();

                        Reflection.SetPrivateField(instance, "spriteId", spriteId);

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

            if (image.sprite != null && image.sprite.name == UIImage.DummyAssetName)
            {
                dummySprite = image.sprite;
            }
        }
    }
}
