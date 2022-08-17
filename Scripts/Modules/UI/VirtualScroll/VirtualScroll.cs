
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        public enum ScrollTo
        {
            First,
            Center,
            Last,
        }
    }

    public abstract class VirtualScroll<T> : UIBehaviour where T : class
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
        private bool hitBoxEnable = true;

        private RectTransform scrollRectTransform = null;
        private float itemSize = -1f;
        private float prevScrollPosition = 0f;
        private Tweener centerToTween = null;

        private List<VirtualScrollItem<T>> itemList = null;

		private CancellationTokenSource cancelSource = null;
		private Dictionary<VirtualScrollItem<T>, IDisposable> updateItemDisposables = null;

		private GraphicCast hitBox = null;

        private Subject<Unit> onUpdateContents = null;
        private Subject<IVirtualScrollItem> onCreateItem = null;
        private Subject<IVirtualScrollItem> onUpdateItem = null;

		private Status initialize = Status.None;

        //----- property -----

        public IReadOnlyList<T> Contents { get; protected set; }

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

                UpdateScroll();
            }
        }

        /// <summary> 管理下のリストアイテム. </summary>
        public IEnumerable<VirtualScrollItem<T>> ListItems { get { return itemList; } }

        /// <summary> リストアイテムが存在するか. </summary>
        public bool HasListItem { get { return itemList != null && itemList.Any(); } }

        /// <summary> スクロールの方向 </summary>
        protected Direction Direction { get { return direction; } }

        //----- method -----

        protected virtual void Initialize()
        {
            if (initialize != Status.None) { return; }
			
			updateItemDisposables = new Dictionary<VirtualScrollItem<T>, IDisposable>();

            scrollRectTransform = UnityUtility.GetComponent<RectTransform>(scrollRect.gameObject);

            scrollRect.horizontal = direction == Direction.Horizontal;
            scrollRect.vertical = direction == Direction.Vertical;

            itemList = new List<VirtualScrollItem<T>>();

            ScrollPosition = 0f;

            initialize = Status.Initialize;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Observable.EveryUpdate()
                .TakeUntilDisable(this)
                .Subscribe(_ => UpdateScroll())
                .AddTo(this);
        }

		protected override void OnDestroy()
		{
			if (cancelSource != null)
			{
				cancelSource.Cancel();
			}
		}

		public virtual void SetContents(T[] contents)
        {
            Contents = contents;
        }

        public async UniTask UpdateContents(bool keepScrollPosition = false)
        {
			if (initialize == Status.None)
			{
				Initialize();
			}
			
			Cancel();

			//----- Contentのサイズ設定 -----

            var scrollPosition = ScrollPosition;

            if (itemSize == -1)
            {
                var rt = UnityUtility.GetComponent<RectTransform>(itemPrefab);

                itemSize = direction == Direction.Vertical ? rt.rect.height : rt.rect.width;
            }

			scrollRect.content.anchorMin = direction == Direction.Vertical ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0f);
            scrollRect.content.anchorMax = direction == Direction.Vertical ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 1f);
            scrollRect.content.pivot = new Vector2(0.5f, 0.5f);

            var delta = scrollRectTransform.rect.size;

            switch (scrollType)
            {
                case ScrollType.Loop:
                    {
                        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

                        // ※ UIScrollViewのautoScrollDisableに引っかからないように領域を拡張.

                        if (direction == Direction.Vertical)
                        {
                            delta.x = 0f;                            
                            delta.y += 1f;
                        }
                        else
                        {
                            delta.y = 0f;
                            delta.x += 1f;
                        }

                        scrollRect.content.sizeDelta = delta;
                    }
                    break;

                case ScrollType.Limited:
                    {
                        if (scrollRect.movementType == ScrollRect.MovementType.Unrestricted)
                        {
                            scrollRect.movementType = ScrollRect.MovementType.Elastic;
                        }

                        var sizeDelta = Mathf.Abs(edgeSpacing) * 2 + itemSize * Contents.Count + itemSpacing * (Contents.Count - 1);

                        if (direction == Direction.Vertical)
                        {
                            var scrollHeight = scrollRectTransform.rect.height;
                            delta.x = 0f;
                            delta.y = sizeDelta < scrollHeight ? scrollHeight : sizeDelta;
                        }
                        else
                        {
                            var scrollWidth = scrollRectTransform.rect.width;
                            delta.x = sizeDelta < scrollWidth ? scrollWidth : sizeDelta;
                            delta.y = 0f;
                        }

                        scrollRect.content.sizeDelta = delta;
                    }
                    break;
            }

            //----- ヒットボックス初期化 -----

            SetupHitBox();

            //----- 足りない分を生成 -----

            var requireCount = GetRequireCount();

            var createCount = requireCount - itemList.Count;

            if (0 < createCount)
            {
                var addItems = UnityUtility.Instantiate<VirtualScrollItem<T>>(scrollRect.content.gameObject, itemPrefab, createCount).ToArray();

                itemList.AddRange(addItems);

                // 全アイテム非アクティブ化.
                addItems.ForEach(x => UnityUtility.SetActive(x, false));

                // 生成したインスタンス初期化.
				try
				{
					var tasks = new UniTask[addItems.Length];

					for (var i = 0; i < addItems.Length; i++)
					{
						var item = addItems[i];

						tasks[i] = UniTask.Defer(() => InitializeItem(item));
					}

					await UniTask.WhenAll(tasks).AttachExternalCancellation(cancelSource.Token);
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

            // 要素数が少ない時はスクロールを無効化.
            scrollRect.enabled = ScrollEnable();

            var scrollbar = direction == Direction.Vertical ? scrollRect.verticalScrollbar : scrollRect.horizontalScrollbar;

            if (scrollbar != null)
            {
                UnityUtility.SetActive(scrollbar.gameObject, ScrollEnable());
            }

			//----- リストアイテム更新 -----

			try
			{
				var tasks = new UniTask[itemList.Count];

				// 配置初期位置(中央揃え想定なのでItemSize * 0.5f分ずらす).
				var basePosition = direction == Direction.Vertical ?
									scrollRect.content.rect.height * 0.5f - itemSize * 0.5f - edgeSpacing :
									-scrollRect.content.rect.width * 0.5f + itemSize * 0.5f + edgeSpacing;

				// 位置、情報を更新.
				for (var i = 0; i < itemList.Count; i++)
				{
					var index = i;
					var item = itemList[i];

					var offset = itemSize * i;

					offset += 0 < i ? itemSpacing * i : 0;

					item.RectTransform.anchoredPosition = direction == Direction.Vertical ?
														new Vector2(0, basePosition - offset) :
														new Vector2(basePosition + offset, 0);

					tasks[i] = UniTask.Defer(() => UpdateItem(cancelSource.Token, item, index));
				}

				await UniTask.WhenAll(tasks).AttachExternalCancellation(cancelSource.Token);
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

            //----- スクロール位置設定 -----

            if (keepScrollPosition)
            {
                ScrollPosition = scrollPosition;
            }
            else
            {
                ScrollToItem(0, ScrollTo.First);
            }

            //----- 並べ替え -----

            UpdateSibling();

            //-----  更新イベント -----

            await OnUpdateContents().AttachExternalCancellation(cancelSource.Token);

            if (onUpdateContents != null)
            {
                onUpdateContents.OnNext(Unit.Default);
            }
        }

        private async UniTask InitializeItem(VirtualScrollItem<T> item)
        {
            UnityUtility.SetActive(item, true);

            await OnCreateItem(item).AttachExternalCancellation(cancelSource.Token);

            if (onCreateItem != null)
            {
                onCreateItem.OnNext(item);
            }

            // 初期化.
			try
			{
				await item.Initialize().AttachExternalCancellation(cancelSource.Token);
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			UnityUtility.SetActive(item, false);
        }

		/// <summary> 実行中の処理を中断 </summary>
		public void Cancel()
		{
			if (cancelSource != null)
			{
				cancelSource.Cancel();
			}

			cancelSource = new CancellationTokenSource();

			if (updateItemDisposables != null)
			{
				foreach (var item in updateItemDisposables.Values)
				{
					item.Dispose();
				}

				updateItemDisposables.Clear();
			}
		}

        private void SetupHitBox()
        {
            if (hitBoxEnable)
            {
                var parent = scrollRect.viewport;

                if (hitBox == null)
                {
                    hitBox = UnityUtility.CreateGameObject<GraphicCast>(parent.gameObject, "HitBox");
                }
                else
                {
                    UnityUtility.SetParent(hitBox, parent);
                }

                // HitBoxは全域使用.

                var rt = hitBox.transform as RectTransform;

                rt.FillRect();
            }
            else
            {
                UnityUtility.DeleteGameObject(hitBox);
            }
        }

        public void ScrollToItem(int index, ScrollTo to)
        {
            prevScrollPosition = GetCurrentPosition();

            var anchoredPosition = GetScrollToPosition(index, to);

            scrollRect.content.anchoredPosition = anchoredPosition;

            UpdateScroll();

            OnMoveEnd();
        }

        public async UniTask ScrollToItem(int index, ScrollTo to, float duration, Ease ease = Ease.Unset)
        {
            prevScrollPosition = GetCurrentPosition();

            var targetPosition = GetScrollToPosition(index, to);

            var currentPosition = scrollRect.content.anchoredPosition;

            if (centerToTween != null)
            {
                centerToTween.Kill();
                centerToTween = null;
            }

            switch (direction)
            {
                case Direction.Vertical:
                    centerToTween = DOTween.To(() => currentPosition.y,
                            x => scrollRect.content.anchoredPosition = Vector.SetY(scrollRect.content.anchoredPosition, x),
                            targetPosition.y,
                            duration)
                        .SetEase(ease)
						.SetLink(gameObject);
                    break;

                case Direction.Horizontal:
                    centerToTween = DOTween.To(() => currentPosition.x,
                            x => scrollRect.content.anchoredPosition = Vector.SetX(scrollRect.content.anchoredPosition, x),
                            targetPosition.x,
                            duration)
                        .SetEase(ease)
						.SetLink(gameObject);
                    break;
            }

            if (centerToTween != null)
			{
	            await centerToTween.Play().ToUniTask().AttachExternalCancellation(cancelSource.Token);

				OnMoveEnd();
			}
        }

		private Vector2 GetScrollToPosition(int index, ScrollTo to)
        {
            var scrollToPosition = GetItemScrollToPosition(index);

            var scrollToOffset = itemSize * 0.5f + edgeSpacing;

            switch (direction)
            {
                case Direction.Vertical:
                    {
                        var contentHeight = scrollRect.content.rect.height;
                        var scrollHeight = scrollRectTransform.rect.height;

                        var top = -contentHeight * 0.5f + scrollHeight * 0.5f;
                        var bottom = contentHeight * 0.5f - scrollHeight * 0.5f;

                        switch (to)
                        {
                            case ScrollTo.First:
                                scrollToPosition.y += scrollHeight * 0.5f;
                                break;

                            case ScrollTo.Last:
                                scrollToPosition.y -= scrollHeight * 0.5f;
                                break;
                        }

                        if (top < scrollToPosition.y + scrollToOffset && scrollToPosition.y - scrollToOffset < bottom || scrollType == ScrollType.Loop)
                        {
                            switch (to)
                            {
                                case ScrollTo.First:
                                    scrollToPosition.y -= scrollToOffset;
                                    break;

                                case ScrollTo.Last:
                                    scrollToPosition.y += scrollToOffset;
                                    break;
                            }
                        }

                        if (scrollType == ScrollType.Limited)
                        {
                            scrollToPosition.y = Mathf.Clamp(scrollToPosition.y, top, bottom);
                        }
                    }
                    break;

                case Direction.Horizontal:
                    {
                        var contentWidth = scrollRect.content.rect.width;
                        var scrollWidth = scrollRectTransform.rect.width;

                        var left = -contentWidth * 0.5f + scrollWidth * 0.5f;
                        var right = contentWidth * 0.5f - scrollWidth * 0.5f;

                        switch (to)
                        {
                            case ScrollTo.First:
                                scrollToPosition.x -= scrollWidth * 0.5f;
                                break;

                            case ScrollTo.Last:
                                scrollToPosition.x += scrollWidth * 0.5f;
                                break;
                        }

                        if (left < scrollToPosition.x + scrollToOffset && scrollToPosition.x - scrollToOffset < right || scrollType == ScrollType.Loop)
                        {
                            switch (to)
                            {
                                case ScrollTo.First:
                                    scrollToPosition.x += scrollToOffset;
                                    break;

                                case ScrollTo.Last:
                                    scrollToPosition.x -= scrollToOffset;
                                    break;
                            }
                        }

                        if (scrollType == ScrollType.Limited)
                        {
                            scrollToPosition.x = Mathf.Clamp(scrollToPosition.x, left, right);
                        }
                    }
                    break;
            }

            return scrollToPosition;
        }
        
        private Vector2 GetItemScrollToPosition(int index)
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
                    {
                        var contentHeight = scrollRect.content.rect.height;

                        anchoredPosition.y = -contentHeight * 0.5f + offset;
                    }
                    break;

                case Direction.Horizontal:
                    {
                        var contentWidth = scrollRect.content.rect.width;

                        anchoredPosition.x = contentWidth * 0.5f - offset;
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

            UpdateScroll();

            scrollRect.StopMovement();
        }

        private void UpdateScroll()
        {
            if (initialize != Status.Done) { return; }

            var scrollPosition = GetCurrentPosition();

            if (scrollPosition == prevScrollPosition) { return; }

            if (!ScrollEnable()) { return; }

            var scrollPlus = prevScrollPosition < scrollPosition;

            ScrollUpdate(scrollPlus);

            prevScrollPosition = scrollPosition;
        }

        public void ScrollUpdate(bool scrollPlus)
        {
            if (!HasListItem){ return; }

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
            var firstItem = itemList.First();
            var lastItem = itemList.Last();

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
                    new Vector3(0f, lastItem.RectTransform.localPosition.y - offset, 0f) :
                    new Vector3(lastItem.RectTransform.localPosition.x + offset, 0f, 0f);

                var updateItemDisposable = updateItemDisposables.GetValueOrDefault(firstItem);

                if (updateItemDisposable != null)
                {
					updateItemDisposable.Dispose();
					updateItemDisposables.Remove(firstItem);
                }
				
				updateItemDisposable = ObservableEx.FromUniTask(cancelToken => UpdateItem(cancelToken, firstItem, lastItem.Index + 1))
					.Subscribe(_ => updateItemDisposables.Remove(firstItem))
					.AddTo(this);

				updateItemDisposables.Add(firstItem, updateItemDisposable);

                UpdateSibling();

                return false;
            }

            return true;
        }

        // 下 / 右 にスクロール.
        private bool ScrollPlus()
        {
            var firstItem = itemList.First();
            var lastItem = itemList.Last();

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
                    new Vector3(0f, firstItem.RectTransform.localPosition.y + offset, 0f) :
                    new Vector3(firstItem.RectTransform.localPosition.x - offset, 0f, 0f);

                var updateItemDisposable = updateItemDisposables.GetValueOrDefault(lastItem);

                if (updateItemDisposable != null)
                {
                    updateItemDisposable.Dispose();
                    updateItemDisposables.Remove(lastItem);
                }

                updateItemDisposable = ObservableEx.FromUniTask(cancelToken => UpdateItem(cancelToken, lastItem, firstItem.Index - 1))
                    .Subscribe(_ => updateItemDisposables.Remove(lastItem))
                    .AddTo(this);

                updateItemDisposables.Add(lastItem, updateItemDisposable);

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
                hitBox.transform.SetAsFirstSibling();
            }
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

        private async UniTask UpdateItem(CancellationToken cancelToken, VirtualScrollItem<T> item, int index)
        {
			if (scrollType == ScrollType.Loop)
            {
				if (index < 0)
				{
					index = Contents.Count - 1;
				}

				if (Contents.Count <= index)
				{
					index = 0;
				}
            }

			item.SetContent(index, Contents);

			if (item.Content != null)
			{
				await item.UpdateContents(item.Content).AttachExternalCancellation(cancelToken);
			}

            UnityUtility.SetActive(item, item.Content != null);

            #if UNITY_EDITOR

            item.transform.name = index.ToString();

            #endif

            await OnUpdateItem(item).AttachExternalCancellation(cancelToken);

			if (onUpdateItem != null)
            {
                onUpdateItem.OnNext(item);
            }
        }

        private static Rect GetWorldRect(RectTransform trans)
        {
            var corners = new Vector3[4];
            trans.GetWorldCorners(corners);

            var tl = corners[0];
            var br = corners[2];

            return new Rect(tl, new Vector2(br.x - tl.x, br.y - tl.y));
        }

        /// <summary> リストアイテム生成時イベント </summary>
        public IObservable<IVirtualScrollItem> OnCreateItemAsObservable()
        {
            return onCreateItem ?? (onCreateItem = new Subject<IVirtualScrollItem>());
        }

        /// <summary> リストアイテム更新時イベント </summary>
        public IObservable<IVirtualScrollItem> OnUpdateItemAsObservable()
        {
            return onUpdateItem ?? (onUpdateItem = new Subject<IVirtualScrollItem>());
        }

        /// <summary> リスト内容更新完了イベント </summary>
        public IObservable<Unit> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<Unit>());
        }

        /// <summary> リストアイテム生成時イベント </summary>
        protected virtual UniTask OnCreateItem(IVirtualScrollItem item) { return UniTask.CompletedTask; }

        /// <summary> リストアイテム更新時イベント </summary>
        protected virtual UniTask OnUpdateItem(IVirtualScrollItem item) { return UniTask.CompletedTask; }

        /// <summary> リスト内容更新完了イベント </summary>
        protected virtual UniTask OnUpdateContents() { return UniTask.CompletedTask; }
    }
}
