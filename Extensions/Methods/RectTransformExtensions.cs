
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Linq;
using Modules.UI.Layout;

namespace Extensions
{
    public static class RectTransformExtensions
    {
        public enum AnchorType
        {
            TopLeft,
            TopCenter,
            TopRight,

            MiddleLeft,
            MiddleCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight,

            VertStretchLeft,
            VertStretchRight,
            VertStretchCenter,

            HorStretchTop,
            HorStretchMiddle,
            HorStretchBottom,

            StretchAll
        }

        public enum PivotPreset
        {
            TopLeft,
            TopCenter,
            TopRight,

            MiddleLeft,
            MiddleCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight,
        }

        private static readonly Dictionary<AnchorType, Vector2> AnchorMinTable = new ()
        {
            { AnchorType.TopLeft,             new Vector2(0.0f, 1.0f) },
            { AnchorType.TopCenter,           new Vector2(0.5f, 1.0f) },
            { AnchorType.TopRight,            new Vector2(1.0f, 1.0f) },
            { AnchorType.MiddleLeft,          new Vector2(0.0f, 0.5f) },
            { AnchorType.MiddleCenter,        new Vector2(0.5f, 0.5f) },
            { AnchorType.MiddleRight,         new Vector2(1.0f, 0.5f) },
            { AnchorType.BottomLeft,          new Vector2(0.0f, 0.0f) },
            { AnchorType.BottomCenter,        new Vector2(0.5f, 0.0f) },
            { AnchorType.BottomRight,         new Vector2(1.0f, 0.0f) },
            { AnchorType.HorStretchTop,       new Vector2(0.0f, 1.0f) },
            { AnchorType.HorStretchMiddle,    new Vector2(0.0f, 0.5f) },
            { AnchorType.HorStretchBottom,    new Vector2(0.0f, 0.0f) },
            { AnchorType.VertStretchLeft,     new Vector2(0.0f, 0.0f) },
            { AnchorType.VertStretchCenter,   new Vector2(0.5f, 0.0f) },
            { AnchorType.VertStretchRight,    new Vector2(1.0f, 0.0f) },
            { AnchorType.StretchAll,          new Vector2(0.0f, 0.0f) },
        };

        private static readonly Dictionary<AnchorType, Vector2> AnchorMaxTable = new ()
        {
            { AnchorType.TopLeft,             new Vector2(0.0f, 1.0f) },
            { AnchorType.TopCenter,           new Vector2(0.5f, 1.0f) },
            { AnchorType.TopRight,            new Vector2(1.0f, 1.0f) },
            { AnchorType.MiddleLeft,          new Vector2(0.0f, 0.5f) },
            { AnchorType.MiddleCenter,        new Vector2(0.5f, 0.5f) },
            { AnchorType.MiddleRight,         new Vector2(1.0f, 0.5f) },
            { AnchorType.BottomLeft,          new Vector2(0.0f, 0.0f) },
            { AnchorType.BottomCenter,        new Vector2(0.5f, 0.0f) },
            { AnchorType.BottomRight,         new Vector2(1.0f, 0.0f) },
            { AnchorType.HorStretchTop,       new Vector2(1.0f, 1.0f) },
            { AnchorType.HorStretchMiddle,    new Vector2(1.0f, 0.5f) },
            { AnchorType.HorStretchBottom,    new Vector2(1.0f, 0.0f) },
            { AnchorType.VertStretchLeft,     new Vector2(0.0f, 1.0f) },
            { AnchorType.VertStretchCenter,   new Vector2(0.5f, 1.0f) },
            { AnchorType.VertStretchRight,    new Vector2(1.0f, 1.0f) },
            { AnchorType.StretchAll,          new Vector2(1.0f, 1.0f) },
        };

        private static readonly Dictionary<PivotPreset, Vector2> PivotTable = new ()
        {
            { PivotPreset.TopLeft,             new Vector2(0.0f, 1.0f) },
            { PivotPreset.TopCenter,           new Vector2(0.5f, 1.0f) },
            { PivotPreset.TopRight,            new Vector2(1.0f, 1.0f) },
            { PivotPreset.MiddleLeft,          new Vector2(0.0f, 0.5f) },
            { PivotPreset.MiddleCenter,        new Vector2(0.5f, 0.5f) },
            { PivotPreset.MiddleRight,         new Vector2(1.0f, 0.5f) },
            { PivotPreset.BottomLeft,          new Vector2(0.0f, 0.0f) },
            { PivotPreset.BottomCenter,        new Vector2(0.5f, 0.0f) },
            { PivotPreset.BottomRight,         new Vector2(1.0f, 0.0f) },
        };

        private static Vector3[] tempCorners = new Vector3[4];

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

        public static void SetPivotAndAnchors(this RectTransform self, Vector2 aVec)
        {
            self.pivot = aVec;
            self.anchorMin = aVec;
            self.anchorMax = aVec;
        }

        public static Vector2 GetSize(this RectTransform self)
        {
            return self.rect.size;
        }

        public static float GetWidth(this RectTransform self)
        {
            return self.rect.width;
        }

        public static float GetHeight(this RectTransform self)
        {
            return self.rect.height;
        }

        /// <summary> アンカーの設定をFillに変更. </summary>
        public static void FillRect(this RectTransform self)
        {
            self.anchorMin = new Vector2(0f, 0f);
            self.anchorMax = new Vector2(1f, 1f);
            self.offsetMin = new Vector2(0f, 0f);
            self.offsetMax = new Vector2(0f, 0f);
        }

        public static void SetAnchor(this RectTransform self, AnchorType anchor, int offsetX = 0, int offsetY = 0)
        {
            self.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            self.anchorMin = AnchorMinTable.GetValueOrDefault(anchor);
            self.anchorMax = AnchorMaxTable.GetValueOrDefault(anchor);
        }

        public static void SetPivot(this RectTransform source, PivotPreset pivot)
        {
            source.pivot = PivotTable.GetValueOrDefault(pivot);
        }

        public static void SetSize(this RectTransform self, Vector2 newSize)
        {
            Vector2 oldSize = self.rect.size;
            Vector2 deltaSize = newSize - oldSize;
            self.offsetMin = self.offsetMin - new Vector2(deltaSize.x * self.pivot.x, deltaSize.y * self.pivot.y);
            self.offsetMax = self.offsetMax + new Vector2(deltaSize.x * (1f - self.pivot.x), deltaSize.y * (1f - self.pivot.y));
        }

        public static void SetWidth(this RectTransform self, float newSize)
        {
            SetSize(self, new Vector2(newSize, self.rect.size.y));
        }

        public static void SetHeight(this RectTransform self, float newSize)
        {
            SetSize(self, new Vector2(self.rect.size.x, newSize));
        }

        public static Vector2 CalcSize(this RectTransform self)
        {
            var rect = new Rect();

            var current = self as Transform;

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

        /// <summary> ワールド座標上でのRectを取得. </summary>
        public static Rect GetWorldRect(this RectTransform self)
        {
            self.GetWorldCorners(tempCorners);

            var tl = tempCorners[0];
            var br = tempCorners[2];

            return new Rect(tl, new Vector2(br.x - tl.x, br.y - tl.y));
        }

        /// <summary> 子階層を含むレイアウトのRectを取得. </summary>
        public static Bounds CalculateRelativeWorldRect(this RectTransform self)
        {
            var gameObjects = UnityUtility.GetChildrenAndSelf(self.gameObject);

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

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(self);

            var center = self.TransformPoint(bounds.center);
            var size = self.TransformVector(bounds.size);

            return new Bounds(center, size);
        }

        public static Vector2 GetPreferredSize(this RectTransform self)
        {
            var width = LayoutUtility.GetPreferredWidth(self);
            var hight = LayoutUtility.GetPreferredHeight(self);

            return new Vector2(width, hight);
        }

        /// <summary> 重なっているかどうか </summary>
        public static bool IsOverlap(this RectTransform rect1, RectTransform rect2) 
        {
            var rect1Corners = new Vector3[4];
            var rect2Corners = new Vector3[4];

            rect1.GetWorldCorners(rect1Corners);
            rect2.GetWorldCorners(rect2Corners);

            for (var i = 0; i < 4; i++) 
            {
                if (IsPointInsideRect(rect1Corners[i], rect2Corners)) 
                {
                    return true;
                }

                if (IsPointInsideRect(rect2Corners[i], rect1Corners))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary> 指定座標が矩形の内部にあるか </summary>
        private static bool IsPointInsideRect(Vector3 point, Vector3[] rectCorners) 
        {
            var inside = false;

            for (int i = 0, j = 3; i < 4; j = i++) 
            {
                if (((rectCorners[i].y > point.y) != (rectCorners[j].y > point.y)) &&
                    (point.x < (rectCorners[j].x - rectCorners[i].x) * (point.y - rectCorners[i].y) / (rectCorners[j].y - rectCorners[i].y) + rectCorners[i].x)) 
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary> 対象のRectTransformを内包しているか </summary>
        public static bool Contains(this RectTransform self, RectTransform target)
        {
            var targetBounds = GetBounds(target);

            return self.Contains(targetBounds);
        }

        /// <summary> 対象のBoundsを内包しているか </summary>
        public static bool Contains(this RectTransform self, Bounds bounds)
        {
            var selfBounds = GetBounds(self);

            return selfBounds.Contains(new Vector3(bounds.min.x, bounds.min.y, 0f)) &&
                    selfBounds.Contains(new Vector3(bounds.max.x, bounds.max.y, 0f)) &&
                    selfBounds.Contains(new Vector3(bounds.min.x, bounds.max.y, 0f)) &&
                    selfBounds.Contains(new Vector3(bounds.max.x, bounds.min.y, 0f));
        }

        /// <summary> 対象のRectTransformに接触しているか </summary>
        public static bool IsHit(this RectTransform self, RectTransform target)
        {
            var targetBounds = GetBounds(target);

            return self.IsHit(targetBounds);
        }

        /// <summary> 対象のBoundsに接触しているか </summary>
        public static bool IsHit(this RectTransform self, Bounds bounds)
        {
            var selfBounds = GetBounds(self);

            return Mathf.Abs(selfBounds.min.x - bounds.min.x) < selfBounds.size.x / 2 + bounds.size.x / 2 &&
                   Mathf.Abs(selfBounds.min.y - bounds.min.y) < selfBounds.size.y / 2 + bounds.size.y / 2;
        }

        private static Bounds GetBounds(this RectTransform self)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            self.GetWorldCorners(tempCorners);

            for (var i = 0; i < 4; i++)
            {
                min = Vector3.Min(tempCorners[i], min);
                max = Vector3.Max(tempCorners[i], max);
            }
 
            max.z = 0f;
            min.z = 0f;
            
            var bounds = new Bounds(min, Vector3.zero);

            bounds.Encapsulate(max);

            return bounds;
        }

        /// <summary> 子階層を含むレイアウトグループを強制更新 </summary>
        public static void ForceRebuildLayoutGroup(this RectTransform self)
        {
            if (UnityUtility.IsNull(self) || UnityUtility.IsNull(self.gameObject)){ return; }

            var layoutGroups = self.gameObject.DescendantsAndSelf()
                .Where(x => !UnityUtility.IsNull(x))
                .Where(x => UnityUtility.IsActiveInHierarchy(x))
                .OfComponent<LayoutGroup>()
                .OrderBy(x => x.layoutPriority)
                .ToArray();

            foreach (var layoutGroup in layoutGroups)
            {
                var rt = layoutGroup.transform as RectTransform;

                try
                {
                    if (UnityUtility.IsNull(rt)){ continue; }

                    if(layoutGroup.IsDestroyed()){ continue; }

                    layoutGroup.SetLayoutHorizontal();
                    layoutGroup.SetLayoutVertical();

                    layoutGroup.CalculateLayoutInputHorizontal();
                    layoutGroup.CalculateLayoutInputVertical();

                    LayoutRebuilder.MarkLayoutForRebuild(rt);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(self);
        }
    }
}
