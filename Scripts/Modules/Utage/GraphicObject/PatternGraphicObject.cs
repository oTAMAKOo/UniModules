
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.PatternTexture;
using Utage;
using UtageExtensions;

using PatternImage = Modules.PatternTexture.PatternImage;

namespace Modules.UtageExtension
{
	public sealed class PatternGraphicObject : AdvGraphicObjectUguiBase
    {
        //----- params -----

        //----- field -----

        private PatternImage patternImage = null;

        //クロスフェード用のファイル参照
        private AssetFileReference crossFadeReference = null;

        //----- property -----

        protected override Material Material
        {
            get { return patternImage.material; }
            set { patternImage.material = value; }
        }

        //----- method -----

        protected override void AddGraphicComponentOnInit()
        {
            patternImage = gameObject.AddComponent<PatternImage>();
        }

        internal override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
        {
            patternImage.material = graphic.RenderTextureSetting.GetRenderMaterialIfEnable(patternImage.material);

            // 既に描画されている場合は、クロスフェード用のイメージを作成.
            var crossFade = TryCreateCrossFadeImage(patternImage.PatternName, fadeTime, graphic);

            if (!crossFade)
            {
                ReleaseCrossFadeReference();
            }

            patternImage.CrossFade = crossFade;

            var patternTexture = graphic.File.UnityObject as Modules.PatternTexture.PatternTexture;

            patternImage.PatternTexture = patternTexture;
            patternImage.PatternName = graphic.SubFileName;
            patternImage.SetNativeSize();

            if (!crossFade)
            {
                ParentObject.FadeIn(fadeTime, () => { });
            }
        }

        // 文字列指定でのパターンチェンジ.
        public override void ChangePattern(string pattern)
        {
            patternImage.PatternName = pattern;
        }

        private bool TryCreateCrossFadeImage(string patternName, float time, AdvGraphicInfo graphic)
        {
            if (LastResource == null) return false;

            if (EnableCrossFade(graphic))
            {
                ReleaseCrossFadeReference();

                crossFadeReference = gameObject.AddComponent<AssetFileReference>();
                crossFadeReference.Init(LastResource.File);

                patternImage.CrossFadeTime = time;

                return true;
            }

            return false;
        }
        
        private void ReleaseCrossFadeReference()
        {
            if (crossFadeReference != null)
            {
                Destroy(crossFadeReference);
                crossFadeReference = null;
            }
        }

        // 今の表示状態と比較して、クロスフェード可能か.
        private bool EnableCrossFade(AdvGraphicInfo graphic)
        {
            var patternTexture = graphic.File.UnityObject as PatternTexture.PatternTexture;

            var pattern = graphic.SubFileName;
            var data = patternImage.GetPatternData(pattern);

            if (data == null) { return false; }

            return patternImage.PatternTexture == patternTexture
                && patternImage.rectTransform.pivot == graphic.Pivot
                && patternImage.Current.Width == data.Width
                && patternImage.Current.Height == data.Height;
        }

        internal override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
        {
            return !EnableCrossFade(graphic);
        }
    }
}

#endif
