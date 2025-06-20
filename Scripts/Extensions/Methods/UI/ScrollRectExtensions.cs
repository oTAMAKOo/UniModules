
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Extensions
{
    public static class ScrollRectExtensions
    {
        public static void ScrollToItem(this ScrollRect scrollRect, GameObject target, Vector2? offset = null)
        {
            var rt = scrollRect.transform as RectTransform;

            rt.ForceRebuildLayoutGroup();

            var p1 = (Vector2)rt.InverseTransformPoint(scrollRect.content.position);
            var p2 = (Vector2)rt.InverseTransformPoint(target.transform.position);

            var anchoredPosition = scrollRect.content.anchoredPosition;

            if (scrollRect.horizontal)
            {
                anchoredPosition.x = p1.x - p2.x + (offset.HasValue ? offset.Value.x : 0f);
            }

            if (scrollRect.vertical)
            {
                anchoredPosition.y = p1.y - p2.y + (offset.HasValue ? offset.Value.y : 0f);
            }

            scrollRect.content.anchoredPosition = anchoredPosition;
        }

        public static async UniTask ScrollToItemAsync(this ScrollRect scrollRect, GameObject target, float duration, 
                                                      Vector2? offset = null, Ease ease = Ease.OutCubic, CancellationToken cancelToken = default)
        {
            var rt = scrollRect.transform as RectTransform;

            rt.ForceRebuildLayoutGroup();

            var p1 = (Vector2)rt.InverseTransformPoint(scrollRect.content.position);
            var p2 = (Vector2)rt.InverseTransformPoint(target.transform.position);

            var targetAnchoredPosition = scrollRect.content.anchoredPosition;

            if (scrollRect.horizontal)
            {
                targetAnchoredPosition.x = p1.x - p2.x + (offset.HasValue ? offset.Value.x : 0f);
            }

            if (scrollRect.vertical)
            {
                targetAnchoredPosition.y = p1.y - p2.y + (offset.HasValue ? offset.Value.y : 0f);
            }

            var tween =  scrollRect.content.DOAnchorPos(targetAnchoredPosition, duration)
                .SetEase(ease)
                .SetUpdate(true) // TimeScale 無視.
                .SetAutoKill(true)
                .Pause();

            // キャンセル対応
            await using (cancelToken.Register(() => tween.Kill()))
            {
                tween.Play();

                try
                {
                    await tween.AsyncWaitForCompletion();
                }
                catch (System.OperationCanceledException)
                {
                    /* Canceled */
                }
            }
        }
    }
}