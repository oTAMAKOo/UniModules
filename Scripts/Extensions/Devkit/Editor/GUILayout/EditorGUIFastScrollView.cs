
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;

namespace Extensions.Devkit
{
    public abstract class EditorGUIFastScrollView<T>
    {
        //----- params -----

        private const float LayoutMargin = 30f;

        public enum Direction
        {
            Vertical,
            Horizontal,
        }

        private class ItemInfo
        {
            public T content { get; private set; }
            public Rect? rect { get; set; }

            public ItemInfo(T content)
            {
                this.content = content;
                rect = null;
            }
        }

        //----- field -----

        private ItemInfo[] itemInfos = null;
        private Rect? scrollRect = null;

        private int? startIndex = null;
        private int? endIndex = null;

        private float startSpace = 0f;
        private float endSpace = 0f;

        private bool requireSkip = false;

        private Subject<Unit> onRepaintRequest = null;

        //----- property -----

        public T[] Contents
        {
            get { return itemInfos.Select(x => x.content).ToArray(); }

            set
            {
                Refresh();

                itemInfos = value == null ?
                    new ItemInfo[0] :
                    value.Select(x => new ItemInfo(x)).ToArray();

                OnContentsUpdate();
            }
        }

        public Vector2 ScrollPosition { get; set; }

        public bool HideHorizontalScrollBar { get; set; }

        public bool HideVerticalScrollBar { get; set; }

        public abstract Direction Type { get; }

        /// <summary> レイアウトの計算が終わっているか </summary>
        public bool IsLayoutUpdating
        {
            get { return !startIndex.HasValue || !endIndex.HasValue; }
        }

        //----- method -----

        public EditorGUIFastScrollView()
        {
            itemInfos = new ItemInfo[0];
            requireSkip = true;
        }

        public void Draw(bool scrollEnable = true, params GUILayoutOption[] options)
        {
            var eventType = Event.current.type;

            // 最初のフレームはスキップして再描画要求する.
            if (requireSkip)
            {
                if (eventType == EventType.Repaint)
                {
                    requireSkip = false;
                    RequestRepaint();
                }

                return;
            }

            if (scrollEnable)
            {
                DrawScrollContents(options);
            }
            else
            {
                DrawContents(options);
            }
        }

        private void LayoutVertical(Vector2 scrollPosition)
        {
            if (!scrollRect.HasValue) { return; }
            
            var layoutBegin = scrollPosition.y - LayoutMargin;
            var layoutEnd = scrollPosition.y + scrollRect.Value.height + LayoutMargin;

            startIndex = 0;
            startSpace = 0f;

            //====== 見える範囲までのスペース ======

            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (layoutBegin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            //====== 見えてる範囲から出るまで検索 ======

            var viewSpace = 0f;

            endIndex = itemInfos.Length;
            endSpace = 0f;

            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (layoutEnd < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            //====== 終端までのスペース ======

            for (var i = endIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                endSpace += itemInfos[i].rect.Value.height;
            }
        }

        private void LayoutHorizontal(Vector2 scrollPosition)
        {
            if (!scrollRect.HasValue) { return; }

            var layoutBegin = scrollPosition.x - LayoutMargin;
            var layoutEnd = scrollPosition.x + scrollRect.Value.width + LayoutMargin;

            startIndex = 0;
            startSpace = 0f;

            //====== 見える範囲までのスペース ======

            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (layoutBegin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            //====== 見えてる範囲から出るまで検索 ======

            var viewSpace = 0f;

            endIndex = itemInfos.Length;
            endSpace = 0f;

            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (layoutEnd < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            //====== 終端までのスペース ======

            for (var i = endIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                endSpace += itemInfos[i].rect.Value.width;
            }
        }
        
        private void DrawScrollContents(params GUILayoutOption[] options)
        {
            var eventType = Event.current.type;

            var isRepaintEvent = eventType == EventType.Repaint;

            var horizontalScrollBar = HideHorizontalScrollBar ? GUIStyle.none : GUI.skin.horizontalScrollbar;

            var verticalScrollBar = HideVerticalScrollBar ? GUIStyle.none : GUI.skin.verticalScrollbar;

            // スクロール領域計測用.
            using (var scrollViewLayoutScope = new EditorGUILayout.VerticalScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(ScrollPosition, false, false, horizontalScrollBar, verticalScrollBar, GUIStyle.none, options))
                {
                    var scrollPosition = scrollViewScope.scrollPosition;
                    
                    if (eventType == EventType.Layout)
                    {
                        switch (Type)
                        {
                            case Direction.Horizontal:
                                LayoutHorizontal(scrollPosition);
                                break;

                            case Direction.Vertical:
                                LayoutVertical(scrollPosition);
                                break;
                        }
                    }

                    using (GetScrollDirectionScope())
                    {
                        GUILayout.Space(startSpace);

                        for (var i = 0; i < itemInfos.Length; i++)
                        {
                            if (startIndex.HasValue && i < startIndex.Value) { continue; }

                            if (endIndex.HasValue && endIndex.Value < i) { continue; }

                            var drawContent = true;

                            if (isRepaintEvent)
                            {
                                var prevItemInfo = itemInfos.ElementAtOrDefault(i - 1);

                                if (prevItemInfo != null && prevItemInfo.rect.HasValue)
                                {
                                    var contentArea = new Rect()
                                    {
                                        xMin = ScrollPosition.x - LayoutMargin,
                                        xMax = ScrollPosition.x + scrollViewLayoutScope.rect.width + LayoutMargin,
                                        yMin = ScrollPosition.y - LayoutMargin,
                                        yMax = ScrollPosition.y + scrollViewLayoutScope.rect.height + LayoutMargin,
                                    };

                                    var isContentAreaOver = contentArea.yMax < prevItemInfo.rect.Value.yMin;

                                    // 描画領域外は描画しない.
                                    if (isContentAreaOver)
                                    {
                                        drawContent = false;
                                    }
                                }
                            }

                            // リストアイテム領域計測用.
                            using (new EditorGUILayout.VerticalScope())
                            {
                                if (drawContent)
                                {
                                    DrawContent(i, itemInfos[i].content);
                                }
                            }

                            if (isRepaintEvent)
                            {
                                itemInfos[i].rect = GUILayoutUtility.GetLastRect();
                            }
                        }

                        GUILayout.Space(endSpace);
                    }

                    ScrollPosition = scrollPosition;
                }
            }

            if (isRepaintEvent)
            {
                scrollRect = GUILayoutUtility.GetLastRect();
            }            
        }

        private void DrawContents(params GUILayoutOption[] options)
        {
            using (GetScrollDirectionScope(options))
            {
                for (var i = 0; i < itemInfos.Length; i++)
                {
                    DrawContent(i, itemInfos[i].content);
                }
            }
        }

        private GUI.Scope GetScrollDirectionScope(params GUILayoutOption[] options)
        {
            GUI.Scope result = null;

            switch (Type)
            {
                case Direction.Horizontal:
                    result = new EditorGUILayout.HorizontalScope(options);
                    break;
                case Direction.Vertical:
                    result = new EditorGUILayout.VerticalScope(options);
                    break;
            }

            return result;
        }

        public void Refresh()
        {
            ScrollPosition = Vector2.zero;
            
            requireSkip = true;

            startIndex = null;
            endIndex = null;

            if (itemInfos != null)
            {
                for (var i = 0; i < itemInfos.Length; i++)
                {
                    itemInfos[i].rect = null;
                }
            }
        }

        /// <summary> 再描画要求イベントを発行 </summary>
        public void RequestRepaint()
        {
            if (onRepaintRequest != null)
            {
                onRepaintRequest.OnNext(Unit.Default);
            }
        }

        /// <summary> コンテンツが更新された時のイベント </summary>
        protected virtual void OnContentsUpdate() { }

        /// <summary> 再描画要求イベント </summary>
        public IObservable<Unit> OnRepaintRequestAsObservable()
        {
            return onRepaintRequest ?? (onRepaintRequest = new Subject<Unit>());
        }

        protected abstract void DrawContent(int index, T content);
    }
}
