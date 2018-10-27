
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ObjectCache;
using UnityEditor.Callbacks;

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

        //----- property -----

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

        protected override Shader GetShader()
        {
            if(outlineShader == null)
            {
                outlineShader = Shader.Find("Custom/UI/Text-Outline");
            }

            return outlineShader;
        }

        protected override void SetShaderParams(Material material)
        {
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_Spread", distance);
        }

        protected override string GetCacheKey()
        {
            return string.Format("{0}.{1}", ColorUtility.ToHtmlStringRGBA(color), distance);
        }
    }
}
