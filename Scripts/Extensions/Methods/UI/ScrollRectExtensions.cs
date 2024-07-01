
using UnityEngine;
using UnityEngine.UI;

namespace Extensions
{
    public static class ScrollRectExtensions
    {
        public static void ScrollToItem(this ScrollRect scrollRect, GameObject target, Vector2? offset = null)
        {
            Canvas.ForceUpdateCanvases();

            var rt = scrollRect.transform as RectTransform;

            var p1 = (Vector2)rt.InverseTransformPoint(scrollRect.content.position);
            var p2 = (Vector2)rt.InverseTransformPoint(target.transform.position);

            scrollRect.content.anchoredPosition = p1 - p2 + (offset.HasValue ? offset.Value : Vector2.zero);
        }
    }
}