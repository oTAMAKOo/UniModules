
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.UI
{
    public sealed class ProgressBar : MonoBehaviour
    {
        //----- params -----

        public enum FillMode
        {
            Filled,
            Resize,
            Sprites
        }
		
        public enum FillSizing
        {
            Parent,
            Fixed,
        }

        //----- field -----

        [SerializeField] 
        private FillMode fillMode = FillMode.Filled;
        [SerializeField] 
        private Image targetImage = null;
        [SerializeField] 
        private Sprite[] sprites = null;
        [SerializeField] 
        private RectTransform targetTransform = null;
        [SerializeField] 
        private FillSizing fillSizing = FillSizing.Parent;
        [SerializeField]
        private float minWidth = 0f;
        [SerializeField]
        private float maxWidth = 100f;
        [SerializeField][Range(0f, 1f)] 
        private float fillAmount = 1f;
        [SerializeField] 
        private long steps = 0;
		
        private Subject<float> onValueChanged = null;

        private bool initialized = false;

        //----- property -----

        public FillMode Mode 
        {
            get { return fillMode; }
            set { fillMode = value; }
        }
        
        public Image TargetImage 
        {
            get { return targetImage; }
            set { targetImage = value; }
        }
	
        public Sprite[] Sprites
        {
            get { return sprites; }
            set { sprites = value; }
        }

        public RectTransform RargetTransform 
        {
            get { return targetTransform; }
            set { targetTransform = value; }
        }

        public float MinWidth 
        {
            get { return minWidth; }
            set 
            {
                minWidth = value;

                UpdateBarFill();
            }
        }

        public float MaxWidth 
        {
            get { return maxWidth; }
            set 
            {
                maxWidth = value;

                UpdateBarFill();
            }
        }

        public float FillAmount 
        {
            get { return fillAmount; }

            set 
            {
                if (fillAmount != Mathf.Clamp01(value))
                {
                    fillAmount = Mathf.Clamp01(value);

                    UpdateBarFill();
                    ValueChangeEvent();
                }
            }
        }

        public long Steps 
        {
            get { return steps; }
            set { steps = value; }
        }

        public long CurrentStep 
        {
            get 
            {
                if (steps == 0){ return 0; }
				
                var perStep = 1f / (steps - 1);

                return Mathf.RoundToInt(fillAmount / perStep);
            }

            set
            {
                if (steps > 0)
                {
                    var perStep = 1f / (steps - 1);

                    fillAmount = Mathf.Clamp(value, 0, steps) * perStep;
                }
            }
        }

        //----- method -----
		
        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized) { return; }

            if (fillMode == FillMode.Resize && fillSizing == FillSizing.Parent && targetTransform != null)
            {
                var height = targetTransform.rect.height;

                targetTransform.anchorMin = targetTransform.pivot;
                targetTransform.anchorMax = targetTransform.pivot;
                targetTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            UpdateBarFill();

            initialized = true;
        }

        void OnRectTransformDimensionsChange()
        {
            UpdateBarFill();
        }

		public void UpdateBarFill()
		{
            if (fillMode == FillMode.Filled && targetImage == null){ return; }

            if (fillMode == FillMode.Resize && targetTransform == null){ return; }

            if (fillMode == FillMode.Sprites && sprites.Length == 0){ return; }

            var fill = fillAmount;
			
			if (steps > 0)
            {
				fill = Mathf.Round(fillAmount * (steps - 1)) / (steps - 1);
            }

			if (fillMode == FillMode.Resize)
			{
                if (fillSizing == FillSizing.Fixed)
                {
                    var size = minWidth + (maxWidth - minWidth) * fill;

                    targetTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                }
                else
                {
                    var parentRt = targetTransform.parent as RectTransform;

                    var size = parentRt.rect.width * fill;

                    targetTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                }
			}
            else if (fillMode == FillMode.Sprites)
            {
                var spriteIndex = Mathf.RoundToInt(fill * sprites.Length) - 1;

                if (spriteIndex > -1)
                {
                    targetImage.overrideSprite = sprites[spriteIndex];
                    targetImage.canvasRenderer.SetAlpha(1f);
                }
                else
                {
                    targetImage.overrideSprite = null;
                    targetImage.canvasRenderer.SetAlpha(0f);
                }
            }
			else
			{
				targetImage.fillAmount = fill;
			}
		}
        
		public void AddFill()
		{
			if (steps > 0)
			{
				CurrentStep += 1;
			}
			else
			{
				FillAmount += 0.1f;
			}

            ValueChangeEvent();
		}
		
		public void RemoveFill()
		{
			if (steps > 0)
			{
				CurrentStep -= 1;
			}
			else
			{
                FillAmount -= 0.1f;
			}

            ValueChangeEvent();
		}

        public void ValueChangeEvent()
        {
            if (onValueChanged != null)
            {
                onValueChanged.OnNext(fillAmount);
            }
        }

        public IObservable<float> OnValueChangedAsObservable()
        {
            return onValueChanged ?? (onValueChanged = new Subject<float>());
        }

        #if UNITY_EDITOR

        void OnValidate()
        {
            if (fillMode == FillMode.Resize && fillSizing == FillSizing.Parent && targetTransform != null)
            {
                var height = targetTransform.rect.height;

                targetTransform.anchorMin = targetTransform.pivot;
                targetTransform.anchorMax = targetTransform.pivot;
                
                targetTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }
		
        #endif
    }
}