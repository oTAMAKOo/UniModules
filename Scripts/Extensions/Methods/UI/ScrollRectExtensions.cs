
using UnityEngine;
using UnityEngine.UI;

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
    }
}