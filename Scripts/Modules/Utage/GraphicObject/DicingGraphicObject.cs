
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Dicing;
using Utage;
using UtageExtensions;

using DicingImage = Modules.Dicing.DicingImage;

namespace Modules.UtageExtension
{
	public class DicingGraphicObject : AdvGraphicObjectUguiBase
    {
        //----- params -----

        //----- field -----

        private DicingImage dicingImage = null;

        //クロスフェード用のファイル参照
        private AssetFileReference crossFadeReference = null;

        //----- property -----

        protected override Material Material
        {
            get { return dicingImage.material; }
            set { dicingImage.material = value; }
        }

        //----- method -----

        protected override void AddGraphicComponentOnInit()
        {
            dicingImage = gameObject.AddComponent<DicingImage>();
        }

        internal override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
        {
            dicingImage.material = graphic.RenderTextureSetting.GetRenderMaterialIfEnable(dicingImage.material);

            // 既に描画されている場合は、クロスフェード用のイメージを作成.
            var crossFade = TryCreateCrossFadeImage(dicingImage.PatternName, fadeTime, graphic);

            if (!crossFade)
            {
                ReleaseCrossFadeReference();
            }

            dicingImage.CrossFade = crossFade;

            var dicingTexture = graphic.File.UnityObject as DicingTexture;

            dicingImage.DicingTexture = dicingTexture;
            dicingImage.PatternName = graphic.SubFileName;
            dicingImage.SetNativeSize();

            if (!crossFade)
            {
                ParentObject.FadeIn(fadeTime, () => { });
            }
        }

        // 文字列指定でのパターンチェンジ.
        public override void ChangePattern(string pattern)
        {
            dicingImage.PatternName = pattern;
        }

        protected bool TryCreateCrossFadeImage(string patternName, float time, AdvGraphicInfo graphic)
        {
            if (LastResource == null) return false;

            if (EnableCrossFade(graphic))
            {
                ReleaseCrossFadeReference();

                crossFadeReference = gameObject.AddComponent<AssetFileReference>();
                crossFadeReference.Init(LastResource.File);

                dicingImage.CrossFadeTime = time;

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
            var dicingTexture = graphic.File.UnityObject as DicingTexture;

            var pattern = graphic.SubFileName;
            var data = dicingImage.GetPatternData(pattern);

            if (data == null) { return false; }

            return dicingImage.DicingTexture == dicingTexture
                && dicingImage.rectTransform.pivot == graphic.Pivot
                && dicingImage.Current.width == data.width
                && dicingImage.Current.height == data.height;
        }

        internal override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
        {
            return !EnableCrossFade(graphic);
        }
    }
}

#endif
