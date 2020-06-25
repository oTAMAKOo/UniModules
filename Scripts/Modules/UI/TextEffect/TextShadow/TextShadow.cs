
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteAlways]
    [DisallowMultipleComponent]
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

        //----- property -----

        public Color Color { get { return color; } }

        public Vector2 Offset { get { return new Vector2(offsetX, offsetY); } }

        //----- method -----

        public void SetColor(Color value)
        {
            color = value;

            TextEffectManager.Instance.Apply(this);
        }

        public void SetOffsetX(float offsetX, float offsetY)
        {
            this.offsetX = offsetX;
            this.offsetY = offsetY;

            TextEffectManager.Instance.Apply(this);
        }

        public override void SetShaderParams(Material material)
        {
            material.SetColor("_ShadowColor", color);
            material.SetVector("_ShadowOffset", new Vector4(offsetX, offsetY));
        }

        public override string GetCacheKey()
        {
            return string.Format("{0}.{1}.{2}", ColorUtility.ToHtmlStringRGBA(color), offsetX, offsetY);
        }
    }
}
