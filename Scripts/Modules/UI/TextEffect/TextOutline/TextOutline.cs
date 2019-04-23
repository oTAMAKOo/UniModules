
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public class TextOutline : TextEffectBase
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField]
        private float distance = 2f;

        private static Shader outlineShader = null;
        private static Shader outlineSoftMaskShader = null;

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

        protected override Shader GetShader(bool softMaskable)
        {
            if(outlineShader == null)
            {
                outlineShader = Shader.Find("Custom/UI/Text-Outline");
            }

            if (outlineSoftMaskShader == null)
            {
                outlineSoftMaskShader = Shader.Find("Custom/UI/Text-Outline (SoftMask)");
            }

            return softMaskable ? outlineSoftMaskShader : outlineShader;
        }

        protected override void SetShaderParams(Material material)
        {
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_Spread", distance);
        }

        protected override string GetCacheKey(bool softMaskable)
        {
            return string.Format("{0}.{1}.{2}", softMaskable, ColorUtility.ToHtmlStringRGBA(color), distance);
        }
    }
}
