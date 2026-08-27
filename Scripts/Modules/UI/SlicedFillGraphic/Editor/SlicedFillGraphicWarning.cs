using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using Extensions.Devkit;

namespace Modules.UI
{
    /// <summary> SlicedFillGraphicの設定不備をインスペクタに警告表示する </summary>
    public static class SlicedFillGraphicWarning
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Draw(SlicedFillGraphic instance)
        {
            if (instance == null){ return; }

            DrawGraphicWarning(instance);

            DrawOrderWarning(instance);
        }

        /// <summary> カット対象のGraphicの設定を検証する </summary>
        private static void DrawGraphicWarning(SlicedFillGraphic instance)
        {
            var graphic = instance.GetComponent<Graphic>();

            if (graphic == null)
            {
                EditorGUISelectableHelpBox.Draw("Graphic component not found.", MessageType.Error);

                return;
            }

            var image = graphic as Image;

            if (image == null)
            {
                EditorGUISelectableHelpBox.Draw($"{graphic.GetType().Name} が生成するメッシュがquadの集合でない場合はカットされません。", MessageType.Info);

                return;
            }

            if (image.type == Image.Type.Filled)
            {
                EditorGUISelectableHelpBox.Draw("Image.type = Filled は Image 自身が fillAmount でカットするため二重制御になります。Sliced または Simple を使用してください。", MessageType.Warning);
            }

            if (image.useSpriteMesh)
            {
                EditorGUISelectableHelpBox.Draw("Use Sprite Mesh のメッシュはquadの集合ではないためカットされません。", MessageType.Warning);
            }

            var sprite = image.sprite;

            if (image.type == Image.Type.Sliced && sprite != null && sprite.border == Vector4.zero)
            {
                EditorGUISelectableHelpBox.Draw($"Sprite({sprite.name}) に Border が設定されていないため Simple と同じ描画になります(カット自体は機能します)。", MessageType.Info);
            }
        }

        /// <summary> メッシュ変更コンポーネントの適用順を検証する </summary>
        private static void DrawOrderWarning(SlicedFillGraphic instance)
        {
            var components = new List<Component>();

            instance.GetComponents(typeof(IMeshModifier), components);

            var index = components.IndexOf(instance);

            if (index == -1){ return; }

            var laterNames = new List<string>();

            for (var i = index + 1; i < components.Count; i++)
            {
                laterNames.Add(components[i].GetType().Name);
            }

            if (laterNames.Count == 0){ return; }

            var message = string.Join(" / ", laterNames);

            EditorGUISelectableHelpBox.Draw(
                $"SlicedFillGraphic より後に別のメッシュ変更コンポーネント({message})があります。\n" +
                "メッシュ変更はコンポーネント順(上から下)に適用されるため、SlicedFillGraphic は最後(一番下)に配置してください。\n" +
                "ColorGradation が後にあるとゲージが縮むほどグラデーションが圧縮され、FlipGraphic が後にあると Fill Origin と見た目の向きが逆になります。",
                MessageType.Warning);
        }
    }
}
