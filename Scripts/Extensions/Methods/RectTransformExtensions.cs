
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Extensions;
using Modules.UI.Layout;
using Unity.Linq;

namespace Extensions
{
    public static class RectTransformExtensions
    {
        public static void Copy(this RectTransform transform, RectTransform source)
        {
            transform.localPosition = source.localPosition;
            transform.localRotation = source.localRotation;
            transform.localScale = source.localScale;

            transform.anchorMin = source.anchorMin;
            transform.anchorMax = source.anchorMax;
            transform.pivot = source.pivot;

            transform.anchoredPosition = source.anchoredPosition;
        }

        public static void SetDefaultScale(this RectTransform trans)
        {
            trans.localScale = new Vector3(1, 1, 1);
        }

        public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec)
        {
            trans.pivot = aVec;
            trans.anchorMin = aVec;
            trans.anchorMax = aVec;
        }

        public static Vector2 GetSize(this RectTransform trans)
        {
            return trans.rect.size;
        }

        public static float GetWidth(this RectTransform trans)
        {
            return trans.rect.width;
        }

        public static float GetHeight(this RectTransform trans)
        {
            return trans.rect.height;
        }

        /// <summary>
        /// アンカーの設定をFillに変更.
        /// </summary>
        public static void FillRect(this RectTransform trans)
        {
            trans.anchorMin = new Vector2(0f, 0f);
            trans.anchorMax = new Vector2(1f, 1f);
            trans.offsetMin = new Vector2(0f, 0f);
            trans.offsetMax = new Vector2(0f, 0f);
        }

        public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
        {
            trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
        }

        public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
        {
            trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
        }
        public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
        {
            trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
        }
        public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
        {
            trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
        }
        public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
        {
            trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
        }

        public static void SetSize(this RectTransform trans, Vector2 newSize)
        {
            Vector2 oldSize = trans.rect.size;
            Vector2 deltaSize = newSize - oldSize;
            trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
            trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
        }
        public static void SetWidth(this RectTransform trans, float newSize)
        {
            SetSize(trans, new Vector2(newSize, trans.rect.size.y));
        }
        public static void SetHeight(this RectTransform trans, float newSize)
        {
            SetSize(trans, new Vector2(trans.rect.size.x, newSize));
        }

        public static Vector2 CalcSize(this RectTransform trans)
        {
            Rect rect = new Rect();

            var current = trans as Transform;

            while (current != null)
            {
                var rectTransform = UnityUtility.GetComponent<RectTransform>(current.gameObject);
                var canvas = UnityUtility.GetComponent<Canvas>(current.gameObject);

                if (canvas != null)
                {
                    return new Vector2(canvas.pixelRect.width - Mathf.Abs(rect.width), canvas.pixelRect.height - Mathf.Abs(rect.height));
                }

                if (rectTransform != null)
                {
                    rect.xMin += rectTransform.rect.xMin;
                    rect.xMax += rectTransform.rect.xMax;

                    rect.yMin += rectTransform.rect.yMin;
                    rect.yMax += rectTransform.rect.yMax;
                }

                current = current.parent;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// ワールド座標上でのRectを取得.
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static Rect GetWorldRect(this RectTransform trans)
        {
            var corners = new Vector3[4];
            trans.GetWorldCorners(corners);

            var tl = corners[0];
            var br = corners[2];

            return new Rect(tl, new Vector2(br.x - tl.x, br.y - tl.y));
        }

        /// <summary>
        /// 子階層を含むレイアウトのRectを取得.
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static Bounds CalculateRelativeWorldRect(this RectTransform trans)
        {
            var gameObjects = trans.gameObject.DescendantsAndSelf().ToArray();

            // ※ 複数のコンポーネントを検証出来る様にforeachで実行.

            foreach (var gameObject in gameObjects)
            {
                var components = gameObject.GetComponents<Component>();

                foreach (var component in components)
                {
                    var preferredSizeCopy = component as PreferredSizeCopy;

                    if (preferredSizeCopy != null)
                    {
                        preferredSizeCopy.UpdateLayoutImmediate();
                    }
                }
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(trans);

            var center = trans.TransformPoint(bounds.center);
            var size = trans.TransformVector(bounds.size);

            return new Bounds(center, size);
        }

        public static Vector2 GetPreferredSize(this RectTransform trans)
        {
            var width = LayoutUtility.GetPreferredWidth(trans);
            var hight = LayoutUtility.GetPreferredHeight(trans);

            return new Vector2(width, hight);
        }
    }
}
