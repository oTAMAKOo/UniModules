
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Modules.UI
{
    [CanEditMultipleObjects, CustomEditor(typeof(ProgressBar), true)]
    public sealed class ProgressBarInspector : Editor
    {
        //----- params -----

        //----- field -----

        private ProgressBar instance = null;

        private SerializedProperty fillModeProperty = null;
        private SerializedProperty targetImageProperty= null;
        private SerializedProperty spritesProperty= null;
        private SerializedProperty targetTransformProperty= null;
        private SerializedProperty fillSizingProperty= null;
        private SerializedProperty minWidthProperty= null;
        private SerializedProperty maxWidthProperty= null;
        private SerializedProperty fillAmountProperty= null;
        private SerializedProperty stepsProperty= null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as ProgressBar;

            fillModeProperty = serializedObject.FindProperty("fillMode");
            targetImageProperty = serializedObject.FindProperty("targetImage");
            spritesProperty = serializedObject.FindProperty("sprites");
            targetTransformProperty = serializedObject.FindProperty("targetTransform");
            fillSizingProperty = serializedObject.FindProperty("fillSizing");
            minWidthProperty = serializedObject.FindProperty("minWidth");
            maxWidthProperty = serializedObject.FindProperty("maxWidth");
            fillAmountProperty = serializedObject.FindProperty("fillAmount");
            stepsProperty = serializedObject.FindProperty("steps");
        }

        public override void OnInspectorGUI()
		{
			var amountChanged = false;
			
			serializedObject.Update();
			
			EditorGUILayout.LabelField("Fill Properties", EditorStyles.boldLabel);
			
            using(new EditorGUI.IndentLevelScope())
            {
			    EditorGUILayout.PropertyField(fillModeProperty, new GUIContent("Fill Type"));

                var fillModeTypes = Enum.GetValues(typeof(ProgressBar.FillMode)).Cast<ProgressBar.FillMode>();

                var fillModeType = fillModeTypes.ElementAtOrDefault(fillModeProperty.enumValueIndex);

                var fillSizingTypes = Enum.GetValues(typeof(ProgressBar.FillSizing)).Cast<ProgressBar.FillSizing>();

                var fillSizingType = fillSizingTypes.ElementAtOrDefault(fillSizingProperty.enumValueIndex);

			    if (fillModeType == ProgressBar.FillMode.Filled)
			    {
				    EditorGUILayout.PropertyField(targetImageProperty, new GUIContent("Fill Target"));

				    if (targetImageProperty.objectReferenceValue != null)
				    {
                        var image = targetImageProperty.objectReferenceValue as UnityEngine.UI.Image;
                    
                        if(image.type != UnityEngine.UI.Image.Type.Filled)
                        {
    					    EditorGUILayout.HelpBox("The target image must be of type Filled.", MessageType.Info);
                        }
				    }
			    }
			    else if (fillModeType == ProgressBar.FillMode.Resize)
			    {
				    EditorGUILayout.PropertyField(targetTransformProperty, new GUIContent("Fill Target"));
				    EditorGUILayout.PropertyField(fillSizingProperty, new GUIContent("Fill Sizing"));

				    if (fillSizingType == ProgressBar.FillSizing.Fixed)
				    {
					    EditorGUILayout.PropertyField(minWidthProperty, new GUIContent("Min Width"));
					    EditorGUILayout.PropertyField(maxWidthProperty, new GUIContent("Max Width"));
				    }
			    }
                else if (fillModeType == ProgressBar.FillMode.Sprites)
                {
                    EditorGUILayout.PropertyField(targetImageProperty, new GUIContent("Fill Target"));
                    EditorGUILayout.PropertyField(spritesProperty, new GUIContent("Sprites"), true);
                }

			    EditorGUILayout.PropertyField(stepsProperty, new GUIContent("Steps"));
            }

			EditorGUILayout.Separator();
			
			EditorGUI.BeginChangeCheck();
			
            EditorGUILayout.PropertyField(fillAmountProperty, new GUIContent("Fill Amount"));

			if (EditorGUI.EndChangeCheck())
			{
				amountChanged = true;
			}
			
			EditorGUILayout.Separator();
						
			serializedObject.ApplyModifiedProperties();
			
			if (amountChanged)
			{
                instance.UpdateBarFill();
                instance.ValueChangeEvent();
			}
		}
    }
}
