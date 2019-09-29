
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class TextOutline : TextEffectBase
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField]
        private float distance = 2f;

        private static Shader outlineShader = null;
        private static Shader outlineSoftMaskShader = null;

        private static Shader outlineShadowShader = null;
        private static Shader outlineShadowSoftMaskShader = null;

        //----- property -----

        public Color Color { get { return color; }}

        public float Distance { get { return distance; } }

        //----- method -----

        public void SetColor(Color value)
        {
            color = value;

            Apply();
        }

        public void SetDistance(float value)
        {
            distance = value;

            Apply();
        }

        public override void SetShaderParams(Material material)
        {
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_OutlineSpread", distance);
        }

        public override string GetCacheKey()
        {
            return string.Format("{0}.{1}", ColorUtility.ToHtmlStringRGBA(color), distance);
        }
    }
}
