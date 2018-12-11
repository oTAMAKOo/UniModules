
#if ENABLE_UTAGE

using UnityEngine;
using UniRx;
using Extensions;
using Modules.Animation;
using Modules.Particle;
using Utage;

namespace Modules.UtageExtension
{
    public class EmotionGraphicObject : AdvGraphicObjectPrefabBase
    {
        //----- params -----

        private const string ANIMATION_ROW_NAME = "SubFileName";

        //----- field -----

        private GameObject instance = null;
        private AnimationPlayer animationController = null;
        private float prevAlpha = 0f;

        //----- property -----

        //----- method -----

        public override void Init(AdvGraphicObject parentObject)
        {
            base.Init(parentObject);

            UnityUtility.SetActive(gameObject, false);
        }

        internal override void SetCommandArg(AdvCommand command)
        {
            var parsed = false;
            var pos = Vector3.zero;

            transform.Reset();

            var x = 0f;
            if (command.TryParseCell<float>(AdvColumnName.Arg4, out x))
            {
                pos.x = x;
                parsed = true;
            }

            var y = 0f;
            if (command.TryParseCell<float>(AdvColumnName.Arg5, out y))
            {
                pos.y = y;
                parsed = true;
            }

            if (parsed)
            {
                instance.transform.localPosition = pos;
            }
        }

        protected override void ChangeResourceOnDrawSub(AdvGraphicInfo grapic)
        {
            var settingData = grapic.File.SettingData;

            if(settingData != null)
            {
                if(instance == null)
                {
                    instance = currentObject;

                    animationController = UnityUtility.GetComponent<AnimationPlayer>(currentObject);
                }

                var animationName = settingData.RowData.ParseCellOptional<string>(ANIMATION_ROW_NAME, null);

                var enable = !string.IsNullOrEmpty(animationName);
                
                UnityUtility.SetActive(gameObject, enable);

                if (enable)
                {
                    animationController.Play(animationName).Subscribe().AddTo(this);
                }
            }
            else
            {
                UnityUtility.SetActive(gameObject, false);
            }
        }

        internal override void OnEffectColorsChange(AdvEffectColor color)
        {
            if (prevAlpha == 1f && color.MulColor.a < prevAlpha)
            {
                UnityUtility.SetActive(instance, false);
            }

            prevAlpha = color.MulColor.a;
        }

        protected void SetSortingOrder(int sortingOrder, string sortingLayerName)
        {
            var particleControllers = currentObject.GetComponentsInChildren<ParticlePlayer>(true);

            foreach (var particleController in particleControllers)
            {
                particleController.SortingOrder += sortingOrder;
            }

            var canvas = currentObject.GetComponentsInChildren<Canvas>(true);

            foreach (var item in canvas)
            {
                item.overrideSorting = true;
                item.sortingOrder += sortingOrder;
            }
        }
    }
}

#endif
