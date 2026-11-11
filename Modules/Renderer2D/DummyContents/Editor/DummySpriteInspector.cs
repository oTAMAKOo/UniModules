
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Renderer2D.DummyContent
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

            var spriteRenderer = instance.SpriteRenderer;

            if (spriteRenderer.sprite != null && spriteRenderer.sprite.name == DummySprite.DummyAssetName)
            {
                dummySprite = spriteRenderer.sprite;
            }
        }

        public override void OnInspectorGUI()
        {
            var instance = target as DummySprite;

            var spriteRenderer = instance.SpriteRenderer;

            if (spriteRenderer == null) { return; }

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
                            assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        }

                        spriteId = spriteAsset.GetSpriteID().ToString();
                    }

                    Reflection.SetPrivateField(instance, "assetGuid", assetGuid);

                    Reflection.SetPrivateField(instance, "spriteId", spriteId);

                    if (spriteAsset == null)
                    {
                        if (spriteRenderer.sprite != null && spriteRenderer.sprite.name == DummySprite.DummyAssetName)
                        {
                            spriteRenderer.sprite = null;
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
                if (spriteRenderer.sprite != dummySprite)
                {
                    UnityUtility.SafeDelete(dummySprite);

                    dummySprite = null;
                }
            }

            if (spriteRenderer.sprite != null && spriteRenderer.sprite.name == DummySprite.DummyAssetName)
            {
                dummySprite = spriteRenderer.sprite;
            }
        }
    }
}
