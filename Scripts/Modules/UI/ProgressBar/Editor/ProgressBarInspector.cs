
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Linq;
using Extensions.Devkit;

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
        private SerializedProperty targetSlicedFillProperty= null;
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
            targetSlicedFillProperty = serializedObject.FindProperty("targetSlicedFill");
            fillSizingProperty = serializedObject.FindProperty("fillSizing");
            minWidthProperty = serializedObject.FindProperty("minWidth");
            maxWidthProperty = serializedObject.FindProperty("maxWidth");
            fillAmountProperty = serializedObject.FindProperty("fillAmount");
            stepsProperty = serializedObject.FindProperty("steps");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Fill Properties", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(fillModeProperty, new GUIContent("Fill Type"));

                switch (GetFillMode())
                {
                    case ProgressBar.FillMode.Filled:
                        EditorGUILayout.PropertyField(targetImageProperty, new GUIContent("Fill Target"));
                        break;

                    case ProgressBar.FillMode.Resize:
                        EditorGUILayout.PropertyField(targetTransformProperty, new GUIContent("Fill Target"));
                        EditorGUILayout.PropertyField(fillSizingProperty, new GUIContent("Fill Sizing"));

                        if (GetFillSizing() == ProgressBar.FillSizing.Fixed)
                        {
                            EditorGUILayout.PropertyField(minWidthProperty, new GUIContent("Min Width"));
                            EditorGUILayout.PropertyField(maxWidthProperty, new GUIContent("Max Width"));
                        }
                        break;

                    case ProgressBar.FillMode.Sprites:
                        EditorGUILayout.PropertyField(targetImageProperty, new GUIContent("Fill Target"));
                        EditorGUILayout.PropertyField(spritesProperty, new GUIContent("Sprites"), true);
                        break;

                    case ProgressBar.FillMode.SlicedFill:
                        EditorGUILayout.PropertyField(targetSlicedFillProperty, new GUIContent("Fill Target"));
                        break;
                }

                EditorGUILayout.PropertyField(stepsProperty, new GUIContent("Steps"));
            }

            var settingChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(fillAmountProperty, new GUIContent("Fill Amount"));

            var amountChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Separator();

            serializedObject.ApplyModifiedProperties();

            if (instance != null)
            {
                if (settingChanged || amountChanged)
                {
                    instance.UpdateBarFill();
                }

                if (amountChanged)
                {
                    instance.ValueChangeEvent();
                }
            }

            DrawWarning();
        }

        private ProgressBar.FillMode GetFillMode()
        {
            var fillModeTypes = Enum.GetValues(typeof(ProgressBar.FillMode)).Cast<ProgressBar.FillMode>();

            return fillModeTypes.ElementAtOrDefault(fillModeProperty.enumValueIndex);
        }

        private ProgressBar.FillSizing GetFillSizing()
        {
            var fillSizingTypes = Enum.GetValues(typeof(ProgressBar.FillSizing)).Cast<ProgressBar.FillSizing>();

            return fillSizingTypes.ElementAtOrDefault(fillSizingProperty.enumValueIndex);
        }

        private void DrawWarning()
        {
            switch (GetFillMode())
            {
                case ProgressBar.FillMode.Filled:
                    DrawFilledWarning();
                    break;

                case ProgressBar.FillMode.Resize:
                    DrawResizeWarning();
                    break;

                case ProgressBar.FillMode.Sprites:
                    DrawSpritesWarning();
                    break;

                case ProgressBar.FillMode.SlicedFill:
                    DrawSlicedFillWarning();
                    break;
            }
        }

        private void DrawFilledWarning()
        {
            var image = targetImageProperty.objectReferenceValue as Image;

            if (image == null)
            {
                EditorGUISelectableHelpBox.Draw("Fill Target が未設定です。", MessageType.Warning);

                return;
            }

            if (image.type != Image.Type.Filled)
            {
                EditorGUISelectableHelpBox.Draw(
                    "Filled は Image.type = Filled でのみ機能します(Sliced / Simple では fillAmount が無視されます)。\n" +
                    "Sliced のままゲージにする場合は Fill Type = SlicedFill を使用してください。",
                    MessageType.Warning);

                return;
            }

            var sprite = image.sprite;

            if (sprite != null && sprite.border != Vector4.zero)
            {
                EditorGUISelectableHelpBox.Draw(
                    $"Filled は 9スライスの Border を無視して Sprite 全体を引き伸ばします(Sprite({sprite.name}) の Border: {sprite.border})。\n" +
                    "端の見た目を維持する場合は Fill Type = SlicedFill を使用してください。",
                    MessageType.Info);
            }
        }

        private void DrawResizeWarning()
        {
            var targetTransform = targetTransformProperty.objectReferenceValue as RectTransform;

            if (targetTransform == null)
            {
                EditorGUISelectableHelpBox.Draw("Fill Target が未設定です。", MessageType.Warning);

                return;
            }

            if (targetTransform.pivot.x != 0f)
            {
                EditorGUISelectableHelpBox.Draw(
                    $"Fill Target の pivot.x が {targetTransform.pivot.x} のため pivot を基準に伸縮します。\n" +
                    "左端から伸ばす場合は pivot.x = 0 に設定してください。",
                    MessageType.Warning);
            }

            if (GetFillSizing() == ProgressBar.FillSizing.Parent)
            {
                var parentRt = targetTransform.parent as RectTransform;

                if (parentRt == null)
                {
                    EditorGUISelectableHelpBox.Draw("Fill Sizing = Parent の基準になる親の RectTransform がありません。", MessageType.Error);
                }
                else if (instance != null && instance.transform != parentRt)
                {
                    EditorGUISelectableHelpBox.Draw(
                        $"Fill Sizing = Parent の基準は Fill Target の親({parentRt.name})です。\n" +
                        "ProgressBar が基準とは別の GameObject にあるため、基準のサイズ変更が自動反映されません。サイズ変更後は UpdateBarFill() を呼んでください。",
                        MessageType.Info);
                }
            }

            var image = targetTransform.GetComponent<Image>();

            if (image == null){ return; }

            if (image.type != Image.Type.Sliced){ return; }

            var sprite = image.sprite;

            if (sprite == null){ return; }

            var horizontalBorder = sprite.border.x + sprite.border.z;

            if (horizontalBorder <= 0f){ return; }

            EditorGUISelectableHelpBox.Draw(
                $"Sprite({sprite.name}) の左右 Border 合計は {horizontalBorder}px です。\n" +
                "Resize は幅を縮めるため、幅がこの値を下回ると Unity が Border を縮小して端が潰れます。また Sprite の長手方向の絵柄も圧縮されます。\n" +
                "潰したくない場合は Fill Type = SlicedFill を使用してください。",
                MessageType.Info);
        }

        private void DrawSpritesWarning()
        {
            if (targetImageProperty.objectReferenceValue == null)
            {
                EditorGUISelectableHelpBox.Draw("Fill Target が未設定です。", MessageType.Warning);
            }

            if (spritesProperty.arraySize == 0)
            {
                EditorGUISelectableHelpBox.Draw("Sprites が未設定です。", MessageType.Warning);
            }
        }

        private void DrawSlicedFillWarning()
        {
            var slicedFill = targetSlicedFillProperty.objectReferenceValue as SlicedFillGraphic;

            if (slicedFill == null)
            {
                EditorGUISelectableHelpBox.Draw(
                    "Fill Target が未設定です。\n" +
                    "ゲージ画像の GameObject に SlicedFillGraphic をアタッチして設定してください(他のメッシュ変更コンポーネントより下に配置)。",
                    MessageType.Warning);

                return;
            }

            SlicedFillGraphicWarning.Draw(slicedFill);
        }
    }
}
