
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Extension
{
    [CustomEditor(typeof(UIImage), true)]
    public sealed class UIImageInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private Sprite spriteAsset = null;

        private Sprite developmentSprite = null;

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

            if (instance.sprite != null && instance.sprite.name == UIImage.DevelopmentAssetName)
            {
                developmentSprite = instance.sprite;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIImage;

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Development Sprite");

                EditorGUI.BeginChangeCheck();

                spriteAsset = EditorGUILayout.ObjectField(spriteAsset, typeof(Sprite), false) as Sprite;

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("UIImageInspector-undo", instance);

                    var assetGuid = spriteAsset != null ? UnityEditorUtility.GetAssetGUID(spriteAsset.texture) : string.Empty;

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    var spriteId = spriteAsset != null ? spriteAsset.GetSpriteID().ToString() : string.Empty;

                    Reflection.SetPrivateField(instance, "spriteId", spriteId);

                    if (spriteAsset == null)
                    {
                        if (instance.sprite != null && instance.sprite.name == UIImage.DevelopmentAssetName)
                        {
                            instance.sprite = null;
                        }
                    }
                    else
                    {
                        Reflection.InvokePrivateMethod(instance, "ApplyDevelopmentAsset");
                    }
                }
            }

            if (developmentSprite != null)
            {
                if (instance.sprite != developmentSprite)
                {
                    UnityUtility.SafeDelete(developmentSprite);

                    developmentSprite = null;
                }
            }

            if (instance.sprite != null && instance.sprite.name == UIImage.DevelopmentAssetName)
            {
                developmentSprite = instance.sprite;
            }
        }
    }
}
