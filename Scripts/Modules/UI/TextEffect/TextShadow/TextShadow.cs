
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.TextEffect
{
    // ※ このComponentを使用するにはTextに指定されたFontのmetaデータのcharacterPaddingを4以上に変更する必要があります.

    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public class TextShadow : TextEffectBase
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField]
        private float offsetX = -1f;
        [SerializeField]
        private float offsetY = -1f;

        private static Shader outlineShader = null;

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

        protected override Shader GetShader()
        {
            if (outlineShader == null)
            {
                outlineShader = Shader.Find("Custom/UI/Text-Shadow");
            }

            return outlineShader;
        }

        protected override void SetShaderParams(Material material)
        {
            material.SetColor("_ShadowColor", color);
            material.SetVector("_Offset", new Vector4(offsetX, offsetY));
        }

        protected override string GetCacheKey()
        {
            return string.Format("{0}.{1}.{2}", ColorUtility.ToHtmlStringRGBA(color), offsetX, offsetY);
        }
    }
}
