
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.UI.SpriteNumber
{
    public abstract class SpriteNumberBase<T> : MonoBehaviour  where T : Component
    {
        //----- params -----

        public enum LayoutType : int
        {
            Center = 1,
            Left   = 2,
            Right  = 3,
        }

        [Serializable]
        private sealed class SpriteData
        {
            public char character = default;

            public Sprite sprite = null;
        }

        //----- field -----
        
        [SerializeField]
        private Transform numberRoot = null;
        [SerializeField]
        private RuntimeAnimatorController animationController = null;
        [SerializeField]
        private SpriteData[] spriteData = null;
        [SerializeField]
        private Color color = Color.white;
        [SerializeField]
        private LayoutType layoutType = LayoutType.Center;

        [SerializeField]
        private float span = 1.0f;
        [SerializeField, Range(0.1f, 5.0f)]
        private float animationSpeed = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)]
        private float animationDelaySeconds = 0.14f;
        [SerializeField, Range(0.0f, 5.0f)]
        private float hideDelaySeconds = 0.6f;

        private List<T> components = null;

        private Dictionary<char, Sprite> spriteTable = null;

        private Dictionary<T, SpriteNumberAnimation> spriteNumberAnimationCache = null;
        private Dictionary<T, Animator> animatorCache = null;

        private Subject<Unit> onAnimationStart = null;
        private Subject<Unit> onAnimationFinish = null;

        private bool initialized = false;

        //----- property -----

        public string Text { get; private set; }

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }

            components = new List<T>();

            spriteNumberAnimationCache = new Dictionary<T, SpriteNumberAnimation>();
            animatorCache = new Dictionary<T, Animator>();

            spriteTable = spriteData.ToDictionary(x => x.character, x => x.sprite);

            OnInitialize();

            initialized = true;
        }

        protected virtual void OnInitialize(){  }
        
        private void BuildContents()
        {
            Initialize();

            if (Text.Length <= components.Count){ return; }

            if (components.IsEmpty())
            {
                var component = UnityUtility.CreateGameObject<T>(numberRoot.gameObject, "origin");

                components.Add(component);
            }

            var addCount = Text.Length - components.Count;

            var origin = components.FirstOrDefault();

            var items = UnityUtility.Instantiate<T>(numberRoot.gameObject, origin, addCount);

            items.ForEach(x => components.Add(x));

            for (var i = 0; i < components.Count; i++)
            {
                components[i].transform.name = i.ToString();
            }
        }

        /// <summary> テキストのカラーを設定 </summary>
        public void SetColor(Color color)
        {
            this.color = color;

            BuildContents();

            UpdateSprites();
        }

        /// <summary> テキストの表示レイアウトを設定 </summary>
        public void SetLayout(LayoutType layoutType)
        {
            this.layoutType = layoutType;

            BuildContents();

            UpdateLayouts();
        }

        /// <summary> テキストの文字と文字の間隔を設定 </summary>
        public void SetSpan(float span)
        {
            this.span = span;

            BuildContents();

            UpdateLayouts();
        }

        /// <summary> 表示するテキストを設定 </summary>
        public void Set(string text, bool forceUpdate = false)
        {
            Initialize();

            if (!forceUpdate && Text == text) { return; }

            ClearText();

            this.Text = text;

            // すべてのコンポーネントを更新します
            UpdateComponents();
        }

        /// <summary> 表示しているテキストをクリア </summary>
        public void ClearText()
        {
            Initialize();

            components.ForEach(x => UnityUtility.SetActive(x, false));
        }

        private void UpdateComponents()
        {
            BuildContents();

            UpdateSprites();
            UpdateLayouts();
            UpdateSizeDelta();

            if (animationController != null)
            {
                PlayAnimation().Forget(this);
            }
        }

        private void UpdateSprites()
        {
            if (string.IsNullOrEmpty(Text)){ return; }

            if (spriteTable == null){ return; }

            var textLength = Text.Length;

            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];

                if (i < textLength)
                {
                    var charStr = Text.ElementAtOrDefault(i);

                    var sprite = spriteTable.GetValueOrDefault(charStr);

                    UpdateComponent(component, sprite, color);
                }

                UnityUtility.SetActive(component, i < textLength);
            }
        }

        private void UpdateLayouts()
        {
            if (components == null){ return; }

            var textLenght = Text.Length;

            for (var i = 0; i < components.Count; i++)
            {
                var rt = components[i].transform as RectTransform;

                if (rt == null){ continue; }

                var position = Vector3.zero;
                var width = rt.sizeDelta.x;

                switch (layoutType)
                {
                    case LayoutType.Center:
                        position.x = (i - (textLenght - 1) / 2.0f) * width * span;
                        break;
                    case LayoutType.Left:
                        position.x = i * width * span;
                        break;
                    case LayoutType.Right:
                        position.x = -(textLenght - 1 - i) * width * span;
                        break;
                }

                rt.localPosition = position;
            }
        }

        private void UpdateSizeDelta()
        {
            var rt = transform as RectTransform;

            rt.sizeDelta = Vector2.zero;

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(numberRoot);

            rt.sizeDelta = bounds.size;
        }

        private async UniTask PlayAnimation()
        {
            var tasks = new List<UniTask>();

            var textLength = Text.Length;

            for (var i = 0; i < components.Count; i++)
            {
                if (textLength <= i){ break; }

                var index = i;
                var component = components[i];

                var task = UniTask.Defer(() => NumberAnimation(component, index));

                tasks.Add(task);

                UnityUtility.SetActive(component, false);
            }

            if (onAnimationStart != null)
            {
                onAnimationStart.OnNext(Unit.Default);
            }

            await UniTask.WhenAll(tasks);

            await UniTask.Delay(TimeSpan.FromSeconds(hideDelaySeconds));

            ClearText();

            if (onAnimationFinish != null)
            {
                onAnimationFinish.OnNext(Unit.Default);
            }
        }

        private async UniTask NumberAnimation(T component, int index)
        {
            var numberAnimation = spriteNumberAnimationCache.GetValueOrDefault(component);

            if (numberAnimation == null)
            {
                numberAnimation = UnityUtility.GetOrAddComponent<SpriteNumberAnimation>(component.gameObject);

                spriteNumberAnimationCache.Add(component, numberAnimation);
            }

            var animator = animatorCache.GetValueOrDefault(component);

            if (animator == null)
            {
                animator = UnityUtility.GetOrAddComponent<Animator>(component.gameObject);

                animatorCache.Add(component, animator);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(index * animationDelaySeconds));

            var animationComplete = false;

            if (numberAnimation != null)
            {
                numberAnimation.Setup(index);
                numberAnimation.OnCompleteAsObservable()
                    .Subscribe(_ => animationComplete = true)
                    .AddTo(this);
            }

            if (animator != null)
            {
                animator.enabled = false;

                animator.runtimeAnimatorController = animationController;
                animator.speed = animationSpeed;
                animator.enabled = true;
            }

            UnityUtility.SetActive(component, true);

            await UniTask.WaitUntil(() => animationComplete);
        }

        protected void Apply()
        {
            if (components == null){ return; }

            components.ForEach(component => OnApply(component));
        }

        public IObservable<Unit> OnAnimationStartAsObservable()
        {
            return onAnimationStart ?? (onAnimationStart  = new Subject<Unit>());
        }

        public IObservable<Unit> OnAnimationFinishAsObservable()
        {
            return onAnimationFinish ?? (onAnimationFinish  = new Subject<Unit>());
        }

        protected abstract void OnApply(T component);

        protected abstract void UpdateComponent(T component, Sprite sprite, Color color);
    }
}