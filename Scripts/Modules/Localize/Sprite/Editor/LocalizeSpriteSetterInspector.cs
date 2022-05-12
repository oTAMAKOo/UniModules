
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.Localize
{
	[CustomEditor(typeof(LocalizeSpriteSetter))]
    public sealed class LocalizeSpriteSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

		private Sprite sprite = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }

			var instance = target as LocalizeSpriteSetter;

			var spriteGuid = Reflection.GetPrivateField<LocalizeSpriteSetter, string>(instance, "spriteGuid");

			if (!string.IsNullOrEmpty(spriteGuid))
			{
				var spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);

				sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
			}

			initialized = true;
		}

		public override void OnInspectorGUI()
		{
			var instance = target as LocalizeSpriteSetter;

			Initialize();

			EditorGUI.BeginChangeCheck();
	                        
			sprite = EditorGUILayout.ObjectField("Sprite", sprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Sprite;
	                    
			if (EditorGUI.EndChangeCheck())
			{
				UnityEditorUtility.RegisterUndo(instance);

				AssetDatabase.TryGetGUIDAndLocalFileIdentifier<Sprite>(sprite, out var spriteGuid, out var localId);

				var spriteName = sprite != null ? sprite.name : string.Empty;

				Reflection.SetPrivateField(instance, "spriteGuid", spriteGuid);
				Reflection.SetPrivateField(instance, "spriteName", spriteName);
			}
		}
    }
}