
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI.ScreenRotation
{
	[CustomEditor(typeof(RotationRoot))]
    public sealed class RotationRootInspector : Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public override void OnInspectorGUI()
		{
			var instance = target as RotationRoot;

			var updateSerializedObject = false;

			GUILayout.Space(2f);

			EditorGUI.BeginChangeCheck();

			var rotateType = (RotateType)EditorGUILayout.EnumPopup("Type", instance.RotateType);

			if (EditorGUI.EndChangeCheck())
			{
				UnityEditorUtility.RegisterUndo(instance);
					
				instance.RotateType = rotateType;
			}
			
			var ignoreManageProperty = serializedObject.FindProperty("ignoreManage");

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(ignoreManageProperty);

			if (EditorGUI.EndChangeCheck())
			{
				UnityEditorUtility.RegisterUndo(instance);

				updateSerializedObject = true;
			}

			if (updateSerializedObject)
			{
				serializedObject.ApplyModifiedProperties();

				serializedObject.Update();
			}
		}
    }
}