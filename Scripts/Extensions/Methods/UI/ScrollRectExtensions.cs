using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Extensions
{
    public static class ScrollRectExtensions
    {
        public enum ScrollAlignment
        {
            Center,
            Top,
            Bottom,
            Left,
            Right
        }

        /// <summary> 指定したアイテムへ即座にスクロール </summary>
        public static void ScrollToItem(this ScrollRect scrollRect, RectTransform item, ScrollAlignment alignment = ScrollAlignment.Center, Vector2 offset = default)
        {
            var target = scrollRect.CalculateAlignedScrollPosition(item, alignment);

            scrollRect.normalizedPosition = scrollRect.ApplyOffset(target, offset);
        }

        /// <summary> 指定したアイテムへアニメ付きでスクロール </summary>
        public static async UniTask ScrollToItemAsync(this ScrollRect scrollRect, RectTransform item, float duration = 0.3f, ScrollAlignment alignment = ScrollAlignment.Center, Vector2 offset = default, Ease ease = Ease.OutQuad, CancellationToken cancelToken = default)
        {
            var target = scrollRect.CalculateAlignedScrollPosition(item, alignment);
            
            await scrollRect.LerpToScrollPosition(scrollRect.ApplyOffset(target, offset), duration, ease, cancelToken);
        }

        private static Vector2 CalculateAlignedScrollPosition(this ScrollRect scrollRect, RectTransform item, ScrollAlignment alignment)
        {
            switch (alignment)
            {
                case ScrollAlignment.Top:
                    return scrollRect.CalculateTopAlignedScrollPosition(item);
                case ScrollAlignment.Bottom:
                    return scrollRect.CalculateBottomAlignedScrollPosition(item);
                case ScrollAlignment.Left:
                    return scrollRect.CalculateLeftAlignedScrollPosition(item);
                case ScrollAlignment.Right:
                    return scrollRect.CalculateRightAlignedScrollPosition(item);

                default:
                    return scrollRect.CalculateFocusedScrollPosition(item);
            }
        }

        private static Vector2 CalculateTopAlignedScrollPosition(this ScrollRect scrollRect, RectTransform item)
        {
            var itemPoint = scrollRect.content.InverseTransformPoint(item.position);
            var scrollPosition = scrollRect.normalizedPosition;

            if (scrollRect.vertical && scrollRect.content.rect.height > scrollRect.viewport.rect.height)
            {
                scrollPosition.y = Mathf.Clamp01((itemPoint.y) / (scrollRect.content.rect.height - scrollRect.viewport.rect.height));
            }

            return scrollPosition;
        }

        private static Vector2 CalculateBottomAlignedScrollPosition(this ScrollRect scrollRect, RectTransform item)
        {
            var itemPoint = scrollRect.content.InverseTransformPoint(item.position);
            var scrollPosition = scrollRect.normalizedPosition;

            if (scrollRect.vertical && scrollRect.content.rect.height > scrollRect.viewport.rect.height)
            {
                scrollPosition.y = Mathf.Clamp01((itemPoint.y - scrollRect.viewport.rect.height) / (scrollRect.content.rect.height - scrollRect.viewport.rect.height));
            }

            return scrollPosition;
        }

        private static Vector2 CalculateLeftAlignedScrollPosition(this ScrollRect scrollRect, RectTransform item)
        {
            var itemPoint = scrollRect.content.InverseTransformPoint(item.position);
            var scrollPosition = scrollRect.normalizedPosition;

            if (scrollRect.horizontal && scrollRect.content.rect.width > scrollRect.viewport.rect.width)
            {
                scrollPosition.x = Mathf.Clamp01((itemPoint.x) / (scrollRect.content.rect.width - scrollRect.viewport.rect.width));
            }

            return scrollPosition;
        }

        private static Vector2 CalculateRightAlignedScrollPosition(this ScrollRect scrollRect, RectTransform item)
        {
            var itemPoint = scrollRect.content.InverseTransformPoint(item.position);
            var scrollPosition = scrollRect.normalizedPosition;

            if (scrollRect.horizontal && scrollRect.content.rect.width > scrollRect.viewport.rect.width)
            {
                scrollPosition.x = Mathf.Clamp01((itemPoint.x - scrollRect.viewport.rect.width) / (scrollRect.content.rect.width - scrollRect.viewport.rect.width));
            }

            return scrollPosition;
        }

        private static Vector2 ApplyOffset(this ScrollRect scrollRect, Vector2 calculatedPosition, Vector2 offset)
        {
            var contentSize = scrollRect.content.rect.size;
            var viewportSize = scrollRect.viewport.rect.size;

            var normalized = calculatedPosition;

            if (scrollRect.horizontal && contentSize.x > viewportSize.x)
            {
                normalized.x = Mathf.Clamp01(normalized.x + offset.x / (contentSize.x - viewportSize.x));
            }

            if (scrollRect.vertical && contentSize.y > viewportSize.y)
            {
                normalized.y = Mathf.Clamp01(normalized.y + offset.y / (contentSize.y - viewportSize.y));
            }

            return normalized;
        }

        public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollRect, RectTransform item)
        {
            var itemCenterPoint = scrollRect.content.InverseTransformPoint(item.TransformPoint(item.rect.center));
            var contentSizeOffset = scrollRect.content.rect.size;
            contentSizeOffset.Scale(scrollRect.content.pivot);
            return scrollRect.CalculateFocusedScrollPosition(itemCenterPoint + contentSizeOffset.ToVector3());
        }

        public static Vector2 CalculateFocusedScrollPosition(this ScrollRect scrollRect, Vector2 focusPoint)
        {
            var contentSize = scrollRect.content.rect.size;
            var viewportSize = scrollRect.viewport.rect.size;

            var scrollPosition = scrollRect.normalizedPosition;

            if (scrollRect.horizontal && contentSize.x > viewportSize.x)
            {
                scrollPosition.x = Mathf.Clamp01((focusPoint.x - viewportSize.x * 0.5f) / (contentSize.x - viewportSize.x));
            }

            if (scrollRect.vertical && contentSize.y > viewportSize.y)
            {
                scrollPosition.y = Mathf.Clamp01((focusPoint.y - viewportSize.y * 0.5f) / (contentSize.y - viewportSize.y));
            }

            return scrollPosition;
        }

        private static async UniTask LerpToScrollPosition(this ScrollRect scrollRect, Vector2 targetNormalizedPos, float duration, Ease ease, CancellationToken cancelToken = default)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            var tween = DOTween.To(() => scrollRect.normalizedPosition, x => scrollRect.normalizedPosition = x, targetNormalizedPos, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetAutoKill(true)
                .Pause();

            using (cancelToken.Register(() => tween.Kill()))
            {
                tween.Play();
                try
                {
                    await tween.AsyncWaitForCompletion();
                }
                catch (System.OperationCanceledException) { }
            }
        }
    }
}
