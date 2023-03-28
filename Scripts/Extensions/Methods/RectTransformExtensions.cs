
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using Unity.Linq;
using Modules.UI.Layout;

namespace Extensions
{
    public static class RectTransformExtensions
    {
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

        /// <summary>
        /// アンカーの設定をFillに変更.
        /// </summary>
        public static void FillRect(this RectTransform self)
        {
			self.anchorMin = new Vector2(0f, 0f);
			self.anchorMax = new Vector2(1f, 1f);
			self.offsetMin = new Vector2(0f, 0f);
			self.offsetMax = new Vector2(0f, 0f);
        }

        public static void SetPositionOfPivot(this RectTransform self, Vector2 newPos)
        {
			self.localPosition = new Vector3(newPos.x, newPos.y, self.localPosition.z);
        }

        public static void SetLeftBottomPosition(this RectTransform self, Vector2 newPos)
        {
			self.localPosition = new Vector3(newPos.x + (self.pivot.x * self.rect.width), newPos.y + (self.pivot.y * self.rect.height), self.localPosition.z);
        }

        public static void SetLeftTopPosition(this RectTransform self, Vector2 newPos)
        {
            self.localPosition = new Vector3(newPos.x + (self.pivot.x * self.rect.width), newPos.y - ((1f - self.pivot.y) * self.rect.height), self.localPosition.z);
        }

        public static void SetRightBottomPosition(this RectTransform self, Vector2 newPos)
        {
            self.localPosition = new Vector3(newPos.x - ((1f - self.pivot.x) * self.rect.width), newPos.y + (self.pivot.y * self.rect.height), self.localPosition.z);
        }

        public static void SetRightTopPosition(this RectTransform self, Vector2 newPos)
        {
            self.localPosition = new Vector3(newPos.x - ((1f - self.pivot.x) * self.rect.width), newPos.y - ((1f - self.pivot.y) * self.rect.height), self.localPosition.z);
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
			var gameObject = self.gameObject;

			if (gameObject == null){ return; }

			IEnumerator LayoutUpdateCore()
			{
				yield return new WaitForEndOfFrame();

				if (self == null){ yield break; }

				if (gameObject == null){ yield break; }
                
				var layoutGroups = gameObject.DescendantsAndSelf().OfComponent<LayoutGroup>();

				foreach (var layoutGroup in layoutGroups)
				{
					layoutGroup.SetLayoutHorizontal();
					layoutGroup.SetLayoutVertical();

					layoutGroup.CalculateLayoutInputHorizontal();
					layoutGroup.CalculateLayoutInputVertical();

					LayoutRebuilder.MarkLayoutForRebuild(layoutGroup.transform as RectTransform);
				}
			}

			Observable.FromCoroutine(() => LayoutUpdateCore())
				.TakeUntilDisable(gameObject)
				.Subscribe()
				.AddTo(gameObject);
		}
    }
}
