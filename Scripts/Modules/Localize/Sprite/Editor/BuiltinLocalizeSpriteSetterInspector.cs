
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Localize
{
	[CustomEditor(typeof(BuiltinLocalizeSpriteSetter), true)]
	public sealed class BuiltininLocalizeSpriteSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public override void OnInspectorGUI()
		{
			var instance = target as BuiltinLocalizeSpriteSetter;

			var changed = false;
			
			var languageType = instance.LanguageType;
			
			var languageNames = Enum.GetNames(languageType);
			var languageEnums = Enum.GetValues(languageType).Cast<Enum>().ToArray();

			var spriteDictionary = Reflection.GetPrivateField<BuiltinLocalizeSpriteSetter, BuiltinLocalizeSpriteSetter.SpriteDictionary>(instance, "spriteDictionary");

			for (var i = 0; i < languageEnums.Length; i++)
			{
				var languageEnum = languageEnums[i];

				EditorGUI.BeginChangeCheck();

				var sprite = spriteDictionary.GetValueOrDefault(languageEnum);

				sprite = EditorGUILayout.ObjectField(languageNames[i], sprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Sprite;

				if (EditorGUI.EndChangeCheck())
				{
					changed = true;

					spriteDictionary[languageEnum] = sprite;
				}
			}

			if (changed)
			{
				Reflection.SetPrivateField(instance, "spriteDictionary", spriteDictionary);
			}
		}
    }
}