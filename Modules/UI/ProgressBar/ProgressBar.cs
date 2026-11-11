
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Modules.UI
{
    public sealed class ProgressBar : MonoBehaviour
    {
        //----- params -----

        public enum FillMode
        {
            Filled,
            Resize,
            Sprites,
            SlicedFill,
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
        private SlicedFillGraphic targetSlicedFill = null;
        [SerializeField]
        private FillSizing fillSizing = FillSizing.Parent;
        [SerializeField]
        private float minWidth = 0f;
        [SerializeField]
        private float maxWidth = 100f;
        [SerializeField]
        [Range(0f, 1f)]
        private float fillAmount = 1f;
        [SerializeField]
        private long steps = 0;

        private Subject<float> onValueChanged = null;

        private bool initialized = false;

        private bool updating = false;

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

        public SlicedFillGraphic TargetSlicedFill
        {
            get { return targetSlicedFill; }
            set { targetSlicedFill = value; }
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
                var amount = Mathf.Clamp01(value);

                if (fillAmount == amount){ return; }

                fillAmount = amount;

                UpdateBarFill();
                ValueChangeEvent();
            }
        }

        public long Steps
        {
            get { return steps; }
            set
            {
                steps = value;

                UpdateBarFill();
            }
        }

        public long CurrentStep
        {
            get
            {
                if (HasSteps() == false){ return 0; }

                var perStep = 1f / (steps - 1);

                return Mathf.RoundToInt(fillAmount / perStep);
            }

            set
            {
                if (HasSteps() == false){ return; }

                var perStep = 1f / (steps - 1);

                // ステップの範囲(0 〜 steps - 1)に丸める.
                var step = Mathf.Clamp(value, 0, steps - 1);

                FillAmount = step * perStep;
            }
        }

        //----- method -----

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized){ return; }

            UpdateBarFill();

            initialized = true;
        }

        void OnRectTransformDimensionsChange()
        {
            UpdateBarFill();
        }

        public void UpdateBarFill()
        {
            // Resizeモードでは自身のRectTransformのサイズ変更を経由して再入する場合があるため多重実行を防ぐ.
            if (updating){ return; }

            updating = true;

            var fill = GetSteppedFillAmount();

            switch (fillMode)
            {
                case FillMode.Filled:
                    UpdateFilled(fill);
                    break;

                case FillMode.Resize:
                    UpdateResize(fill);
                    break;

                case FillMode.Sprites:
                    UpdateSprites(fill);
                    break;

                case FillMode.SlicedFill:
                    UpdateSlicedFill(fill);
                    break;
            }

            updating = false;
        }

        private void UpdateFilled(float fill)
        {
            if (targetImage == null){ return; }

            targetImage.fillAmount = fill;
        }

        private void UpdateResize(float fill)
        {
            if (targetTransform == null){ return; }

            var size = 0f;

            if (fillSizing == FillSizing.Fixed)
            {
                size = minWidth + (maxWidth - minWidth) * fill;
            }
            else
            {
                var parentRt = targetTransform.parent as RectTransform;

                if (parentRt == null)
                {
                    Debug.LogError($"Parent RectTransform not found. ({targetTransform.name}).", this);

                    return;
                }

                size = parentRt.rect.width * fill;
            }

            targetTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        }

        private void UpdateSprites(float fill)
        {
            if (targetImage == null){ return; }

            if (sprites == null){ return; }

            if (sprites.Length == 0){ return; }

            var spriteIndex = Mathf.RoundToInt(fill * sprites.Length) - 1;

            if (-1 < spriteIndex)
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

        private void UpdateSlicedFill(float fill)
        {
            if (targetSlicedFill == null){ return; }

            targetSlicedFill.FillAmount = fill;
        }

        /// <summary> ステップ指定が有効か </summary>
        private bool HasSteps()
        {
            return 1 < steps;
        }

        /// <summary> ステップ数を反映した塗り量を取得 </summary>
        private float GetSteppedFillAmount()
        {
            if (HasSteps() == false){ return fillAmount; }

            return Mathf.Round(fillAmount * (steps - 1)) / (steps - 1);
        }

        public void AddFill()
        {
            if (HasSteps())
            {
                CurrentStep += 1;
            }
            else
            {
                FillAmount += 0.1f;
            }
        }

        public void RemoveFill()
        {
            if (HasSteps())
            {
                CurrentStep -= 1;
            }
            else
            {
                FillAmount -= 0.1f;
            }
        }

        public void ValueChangeEvent()
        {
            if (onValueChanged != null)
            {
                onValueChanged.OnNext(fillAmount);
            }
        }

        public Observable<float> OnValueChangedAsObservable()
        {
            return onValueChanged ?? (onValueChanged = new Subject<float>());
        }
    }
}
