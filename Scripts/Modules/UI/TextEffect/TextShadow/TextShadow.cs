
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public sealed class TextShadow : TextEffectBase
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField]
        private float offsetX = -1f;
        [SerializeField]
        private float offsetY = -1f;

        private static Shader shadowShader = null;
        private static Shader shadowSoftMaskShader = null;

        //----- property -----

        public Color Color { get { return color; } }

        public Vector2 Offset { get { return new Vector2(offsetX, offsetY); } }

        //----- method -----

        public void SetColor(Color value)
        {
            color = value;

            Apply();
        }

        public void SetOffsetX(float offsetX, float offsetY)
        {
            this.offsetX = offsetX;
            this.offsetY = offsetY;

            Apply();
        }

        protected override Shader GetShader(bool softMaskable)
        {
            if (shadowShader == null)
            {
                shadowShader = Shader.Find("Custom/UI/Text-Shadow");
            }

            if (shadowSoftMaskShader)
            {
                shadowSoftMaskShader = Shader.Find("Custom/UI/Text-Shadow (SoftMask)");
            }
            
            return softMaskable ? shadowSoftMaskShader : shadowShader;
        }

        protected override void SetShaderParams(Material material)
        {
            material.SetColor("_ShadowColor", color);
            material.SetVector("_Offset", new Vector4(offsetX, offsetY));
        }

        protected override string GetCacheKey(bool softMaskable)
        {
            return string.Format("{0}.{1}.{2}.{3}", softMaskable, ColorUtility.ToHtmlStringRGBA(color), offsetX, offsetY);
        }
    }
}
