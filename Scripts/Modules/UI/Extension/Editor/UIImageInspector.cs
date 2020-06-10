
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

        private Sprite developmentSprite = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as UIImage;
            
            var assetGuid = Reflection.GetPrivateField<UIImage, string>(instance, "assetGuid");

            var spriteId = Reflection.GetPrivateField<UIImage, string>(instance, "spriteId");

            if (!string.IsNullOrEmpty(assetGuid) && !string.IsNullOrEmpty(spriteId))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                
                if (!string.IsNullOrEmpty(assetPath))
                {
                    developmentSprite = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                        .OfType<Sprite>()
                        .FirstOrDefault(x => x.GetSpriteID().ToString() == spriteId);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as UIImage;

            DrawDefaultScriptlessInspector();

            EditorGUI.BeginChangeCheck();

            developmentSprite = EditorGUILayout.ObjectField("Development Sprite", developmentSprite, typeof(Object), false) as Sprite;

            if (EditorGUI.EndChangeCheck())
            {
                SetAssetGuid(instance, developmentSprite != null ? developmentSprite.texture : null);

                SetSpriteId(instance, developmentSprite);

                Reflection.InvokePrivateMethod(instance, "ApplyDevelopmentAsset");
            }
        }

        private void SetAssetGuid(UIImage instance, UnityEngine.Object asset)
        {
            var assetGuid = string.Empty;

            if (asset != null)
            {
                assetGuid = UnityEditorUtility.GetAssetGUID(asset);
            }

            Reflection.SetPrivateField(instance, "assetGuid", assetGuid);
        }

        private void SetSpriteId(UIImage instance, Sprite sprite)
        {
            var spriteId = string.Empty;

            if (sprite != null)
            {
                spriteId = sprite.GetSpriteID().ToString();
            }

            Reflection.SetPrivateField(instance, "spriteId", spriteId);
        }
    }
}
