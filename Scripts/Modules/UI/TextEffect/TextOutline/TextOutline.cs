
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteAlways]
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

        //----- property -----

        public Color Color { get { return color; }}

        public float Distance { get { return distance; } }

        //----- method -----

        public void SetColor(Color value)
        {
            color = value;

            TextEffectManager.Instance.Apply(this);
        }

        public void SetDistance(float value)
        {
            distance = value;

            TextEffectManager.Instance.Apply(this);
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
