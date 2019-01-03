
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using Extensions;
using Modules.UI.VirtualScroll;

namespace Modules.UI
{
    namespace VirtualScroll
    {
        public enum Direction
        {
            Vertical,
            Horizontal,
        }

        public enum ScrollType
        {
            Limited,
            Loop,
        }
    }

    public abstract class VirtualScroll<T> : UIBehaviour
    {
        //----- params -----

        private enum Status
        {
            None,
            Initialize,
            Done,
        }

        //----- field -----

        [SerializeField]
        private ScrollType scrollType = ScrollType.Limited;
        [SerializeField]
        private Direction direction = Direction.Horizontal;
        [SerializeField]
        private GameObject itemPrefab = null;
        [SerializeField]
        private ScrollRect scrollRect = null;
        [SerializeField, Range(2, 10)]
        private int additionalGeneration = 2;
        [SerializeField]
        private float edgeSpacing = 0f;
        [SerializeField]
        private float itemSpacing = 0f;
        [SerializeField]
        private RectTransform hitBox = null;

        private RectTransform scrollRectTransform = null;
        private float itemSize = -1f;
        private float prevScrollPosition = 0f;
        private Tweener centerToTweener = null;

        private List<VirtualScrollItem<T>> itemList = null;

        private Subject<GameObject> onCreateItem = null;

        private IObservable<Unit> updateQueueing = null;

        private Status initialize = Status.None;

        //----- property -----

        public abstract T[] Contents { get; }

        public ScrollRect ScrollRect { get { return scrollRect; } }

        public float ScrollPosition
        {
            get
            {
                return direction == Direction.Vertical ?
                       scrollRect.content.anchoredPosition.y :
                       scrollRect.content.anchoredPosition.x;
            }

            set
            {
                var scrollPosition = scrollRect.content.anchoredPosition;

                switch (direction)
                {
                    case Direction.Vertical:
                        scrollPosition.y = ScrollEnable() ? value : 0f;
                        break;

                    case Direction.Horizontal:
                        scrollPosition.x = ScrollEnable() ? value : 0f;
                        break;
                }

                scrollRect.content.anchoredPosition = scrollPosition;
            }
        }

        public GameObject[] ListItems { get { return itemList.Select(x => x.gameObject).ToArray(); } }

        //----- method -----

        public IObservable<Unit> UpdateContents()
        {
            // 既に実行中の場合は実行中の物を返す.
            if (updateQueueing != null) { return updateQueueing; }

            if (initialize == Status.None)
            {
                scrollRectTransform = UnityUtility.GetComponent<RectTransform>(scrollRect.gameObject);

                scrollRect.horizontal = direction == Direction.Horizontal;
                scrollRect.vertical = direction == Direction.Vertical;

                itemList = new List<VirtualScrollItem<T>>();

                initialize = Status.Initialize;
            }

            updateQueueing = Observable.FromCoroutine(() => UpdateContentsInternal())
                .Do(_ => initialize = Status.Done)
                .Do(_ => updateQueueing = null)
                .Share();

            return updateQueueing;
        }

        private IEnumerator UpdateContentsInternal()
        {
            if (itemSize == -1)
            {
                var rt = UnityUtility.GetComponent<RectTransform>(itemPrefab);
                itemSize = direction == Direction.Vertical ? rt.rect.height : rt.rect.width;
            }

            // Contentのサイズ設定.
            scrollRect.content.anchorMin = direction == Direction.Vertical ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0f);
            scrollRect.content.anchorMax = direction == Direction.Vertical ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 1f);
            scrollRect.content.pivot = new Vector2(0.5f, 0.5f);

            // Hitboxは全域使用.
            if (hitBox != null)
            {
                hitBox.anchorMin = new Vector2(0f, 0f);
                hitBox.anchorMax = new Vector2(1f, 1f);
                hitBox.pivot = new Vector2(0.5f, 0.5f);
            }

            switch (scrollType)
            {
                case ScrollType.Loop:
                    {
                        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

                        var delta = scrollRectTransform.rect.size;

                        scrollRect.content.sizeDelta = delta;
                    }
                    break;

                case ScrollType.Limited:
                    {
                        if (scrollRect.movementType == ScrollRect.MovementType.Unrestricted)
                        {
                            scrollRect.movementType = ScrollRect.MovementType.Elastic;
                        }

                        var delta = scrollRect.content.sizeDelta;

                        var sizeDelta = Mathf.Abs(edgeSpacing) * 2 + itemSize * Contents.Length + itemSpacing * (Contents.Length - 1);

                        if (direction == Direction.Vertical)
                        {
                            var scrollHeight = scrollRectTransform.rect.height;
                            delta.y = sizeDelta < scrollHeight ? scrollHeight : sizeDelta;
                        }
                        else
                        {
                            var scrollWidth = scrollRectTransform.rect.width;
                            delta.x = sizeDelta < scrollWidth ? scrollWidth : sizeDelta;
                        }

                        scrollRect.content.sizeDelta = delta;
                    }
                    break;
            }

            var requireCount = GetRequireCount();

            // 配置初期位置(中央揃え想定なのでItemSize * 0.5f分ずらす).
            var basePosition = direction == Direction.Vertical ?
                scrollRect.content.rect.height * 0.5f - itemSize * 0.5f - edgeSpacing :
                -scrollRect.content.rect.width * 0.5f + itemSize * 0.5f + edgeSpacing;

            // 足りない分を生成.
            var createCount = requireCount - itemList.Count;
            var addItems = UnityUtility.Instantiate<VirtualScrollItem<T>>(scrollRect.content.gameObject, itemPrefab, createCount, false);

            itemList.AddRange(addItems);

            // 先に登録して非アクティブ化.
            foreach (var item in addItems)
            {
                UnityUtility.SetActive(item, false);
            }

            // 生成したインスタンス初期化.
            foreach (var item in addItems)
            {
                UnityUtility.SetActive(item, true);

                if (onCreateItem != null)
                {
                    onCreateItem.OnNext(item.gameObject);
                }

                yield return item.Initialize().ToYieldInstruction();

                UnityUtility.SetActive(item, false);
            }

            // 要素数が少ない時はスクロールを無効化.
            scrollRect.enabled = ScrollEnable();

            var scrollbar = direction == Direction.Vertical ? scrollRect.verticalScrollbar : scrollRect.horizontalScrollbar;

            if (scrollbar != null)
            {
                UnityUtility.SetActive(scrollbar.gameObject, ScrollEnable());
            }

            // 位置、情報を更新.
            for (var i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];

                var offset = itemSize * i;

                offset += 0 < i ? itemSpacing * i : 0;

                item.RectTransform.anchoredPosition = direction == Direction.Vertical ?
                    new Vector2(0, basePosition - offset) :
                    new Vector2(basePosition + offset, 0);

                UpdateItem(item, i);
            }

            // 並べ替え.
            UpdateSibling();

            // スクロール初期位置設定.
            CenterToItem(0);
        }

        public void CenterToItem(int index)
        {
            prevScrollPosition = GetCurrentPosition();

            var anchoredPosition = CalcCenterToAnchoredPosition(index);

            scrollRect.content.anchoredPosition = anchoredPosition;

            OnMoveEnd();
        }

        public IObservable<Unit> CenterToItem(int index, float duration, Ease ease = Ease.Unset)
        {
            var targetPosition = CalcCenterToAnchoredPosition(index);

            var currentPosition = scrollRect.content.anchoredPosition;

            if (centerToTweener != null)
            {
                centerToTweener.Kill();
                centerToTweener = null;
            }

            switch (direction)
            {
                case Direction.Vertical:
                    centerToTweener = DOTween.To(() => currentPosition.y,
                            x => scrollRect.content.anchoredPosition = Vector.SetY(scrollRect.content.anchoredPosition, x),
                            targetPosition.y,
                            duration)
                        .SetEase(ease);
                    break;

                case Direction.Horizontal:
                    centerToTweener = DOTween.To(() => currentPosition.x,
                            x => scrollRect.content.anchoredPosition = Vector.SetX(scrollRect.content.anchoredPosition, x),
                            targetPosition.x,
                            duration)
                        .SetEase(ease);
                    break;
            }

            if (centerToTweener == null) { return Observable.ReturnUnit(); }

            centerToTweener.Play();

            return Observable.EveryUpdate()
                .SkipWhile(x => centerToTweener.IsPlaying())
                .First()
                .Do(_ => OnMoveEnd())
                .AsUnitObservable();
        }

        private Vector2 CalcCenterToAnchoredPosition(int index)
        {
            var offset = 0f;

            // 端空白.
            offset += edgeSpacing;
            // 中心揃え.
            offset += itemSize * 0.5f;
            // 行間追加.
            offset += 0 < index ? itemSpacing * index : -itemSpacing * index;
            // 対象の中心までのオフセット.
            offset += index * itemSize;

            var anchoredPosition = scrollRect.content.anchoredPosition;

            switch (direction)
            {
                case Direction.Vertical:
                    var contentHeight = scrollRect.content.rect.height;
                    var scrollHeigh = scrollRectTransform.rect.height;

                    switch (scrollType)
                    {
                        case ScrollType.Loop:
                            {
                                anchoredPosition.y = -contentHeight * 0.5f + offset;
                            }
                            break;

                        case ScrollType.Limited:
                            {
                                var bottom = -contentHeight * 0.5f + scrollHeigh * 0.5f;
                                var top = contentHeight * 0.5f - scrollHeigh * 0.5f;

                                anchoredPosition.y = Mathf.Clamp(-contentHeight * 0.5f + offset, bottom, top);
                            }
                            break;
                    }

                    break;

                case Direction.Horizontal:
                    var contentWidth = scrollRect.content.rect.width;
                    var scrollWidth = scrollRectTransform.rect.width;

                    switch (scrollType)
                    {
                        case ScrollType.Loop:
                            {
                                anchoredPosition.x = contentWidth * 0.5f - offset;
                            }
                            break;

                        case ScrollType.Limited:
                            {
                                var left = contentWidth * 0.5f - scrollWidth * 0.5f;
                                var right = -contentWidth * 0.5f + scrollWidth * 0.5f;

                                anchoredPosition.x = Mathf.Clamp(contentWidth * 0.5f - offset, right, left);
                            }
                            break;
                    }

                    break;
            }

            return anchoredPosition;
        }

        private void OnMoveEnd()
        {
            if (scrollType == ScrollType.Limited)
            {
                switch (direction)
                {
                    case Direction.Vertical:
                        if (scrollRect.verticalNormalizedPosition < 0f || 1f < scrollRect.verticalNormalizedPosition)
                        {
                            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
                        }
                        break;

                    case Direction.Horizontal:
                        if (scrollRect.horizontalNormalizedPosition < 0f || 1f < scrollRect.horizontalNormalizedPosition)
                        {
                            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
                        }
                        break;
                }
            }

            UpdateScroll(true);
            UpdateScroll(false);

            scrollRect.StopMovement();
        }

        public void UpdateScroll(bool scrollPlus)
        {
            while (true)
            {
                if (scrollPlus)
                {
                    if (ScrollPlus()) { break; }
                }
                else
                {
                    if (ScrollMinus()) { break; }
                }
            }
        }

        // 上 / 左にスクロール.
        private bool ScrollMinus()
        {
            var firstItem = itemList.FirstOrDefault();
            var lastItem = itemList.LastOrDefault();

            var scrollArea = GetWorldRect(scrollRectTransform);
            var scrollEdge = direction == Direction.Vertical ? scrollArea.yMax : scrollArea.xMin;

            var firstItemArea = GetWorldRect(firstItem.RectTransform);
            var firstItemEdge = direction == Direction.Vertical ? firstItemArea.yMin : firstItemArea.xMax;

            var replace = direction == Direction.Vertical ? scrollEdge < firstItemEdge : firstItemEdge < scrollEdge;

            if (replace)
            {
                itemList.Remove(firstItem);
                itemList.Add(firstItem);

                var offset = itemSize + itemSpacing;

                firstItem.RectTransform.localPosition = direction == Direction.Vertical ?
                    new Vector2(0, lastItem.RectTransform.localPosition.y - offset) :
                    new Vector2(lastItem.RectTransform.localPosition.x + offset, 0);

                UpdateItem(firstItem, lastItem.Index + 1);

                UpdateSibling();

                return false;
            }

            return true;
        }

        // 下 / 右 にスクロール.
        private bool ScrollPlus()
        {
            var firstItem = itemList.FirstOrDefault();
            var lastItem = itemList.LastOrDefault();
            var scrollArea = GetWorldRect(scrollRectTransform);
            var scrollEdge = direction == Direction.Vertical ? scrollArea.yMin : scrollArea.xMax;

            var lastItemArea = GetWorldRect(lastItem.RectTransform);
            var lastItemEdge = direction == Direction.Vertical ? lastItemArea.yMax : lastItemArea.xMin;

            var replace = direction == Direction.Vertical ? lastItemEdge < scrollEdge : scrollEdge < lastItemEdge;

            if (replace)
            {
                itemList.Remove(lastItem);
                itemList.Insert(0, lastItem);

                var offset = itemSize + itemSpacing;

                lastItem.RectTransform.localPosition = direction == Direction.Vertical ?
                    new Vector2(0, firstItem.RectTransform.localPosition.y + offset) :
                    new Vector2(firstItem.RectTransform.localPosition.x - offset, 0);

                UpdateItem(lastItem, firstItem.Index - 1);

                UpdateSibling();

                return false;
            }

            return true;
        }

        private void UpdateSibling()
        {
            foreach (var item in itemList)
            {
                item.transform.SetSiblingIndex(item.Index);
            }

            if (hitBox != null)
            {
                hitBox.SetAsFirstSibling();
            }
        }

        void Update()
        {
            if (initialize != Status.Done) { return; }

            var scrollPosition = GetCurrentPosition();

            if (scrollPosition == prevScrollPosition) { return; }

            if (!ScrollEnable()) { return; }
                        
            UpdateScroll(0 < scrollPosition - prevScrollPosition);

            prevScrollPosition = scrollPosition;
        }

        private float GetCurrentPosition()
        {
            return direction == Direction.Vertical ?
                   -scrollRect.content.anchoredPosition.y :
                   scrollRect.content.anchoredPosition.x;
        }

        private int GetRequireCount()
        {
            var scrollSize = direction == Direction.Vertical ?
                scrollRectTransform.rect.height :
                scrollRectTransform.rect.width;

            // 領域外に予備を作成.
            return (int)(scrollSize / itemSize) + additionalGeneration;
        }

        private bool ScrollEnable()
        {
            var result = false;

            switch (scrollType)
            {
                case ScrollType.Limited:
                    switch (direction)
                    {
                        case Direction.Horizontal:
                            result = scrollRectTransform.rect.width < scrollRect.content.rect.width;
                            break;

                        case Direction.Vertical:
                            result = scrollRectTransform.rect.height < scrollRect.content.rect.height;
                            break;
                    }
                    break;

                case ScrollType.Loop:
                    result = true;
                    break;
            }

            return result;
        }

        private void UpdateItem(VirtualScrollItem<T> item, int index)
        {
            switch (scrollType)
            {
                case ScrollType.Limited:
                    {
                        var enable = Contents != null && 0 <= index && index < Contents.Length;

                        item.UpdateItem(index, Contents);
                        UnityUtility.SetActive(item.gameObject, enable);
                    }
                    break;

                case ScrollType.Loop:
                    {
                        if (index < 0)
                        {
                            index = Contents.Length - 1;
                        }

                        if (Contents.Length <= index)
                        {
                            index = 0;
                        }

                        item.UpdateItem(index, Contents);
                        UnityUtility.SetActive(item.gameObject, true);
                    }
                    break;
            }

            item.transform.name = index.ToString();
        }

        private static Rect GetWorldRect(RectTransform trans)
        {
            var corners = new Vector3[4];
            trans.GetWorldCorners(corners);

            var tl = corners[0];
            var br = corners[2];

            return new Rect(tl, new Vector2(br.x - tl.x, br.y - tl.y));
        }

        public IObservable<GameObject> OnCreateItemAsObservable()
        {
            return onCreateItem ?? (onCreateItem = new Subject<GameObject>());
        }
    }
}
