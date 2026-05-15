
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using R3;
using Extensions;
using Modules.Devkit.AssemblyCompilation;

namespace Extensions.Devkit
{
    /// <summary> 汎用 Sprite 選択ウィンドウ </summary>
    public sealed class SpriteSelectorWindow : EditorWindow
    {
        //----- params -----

        /// <summary> 選択項目 </summary>
        public sealed class Item
        {
            /// <summary> 表示用 Sprite (null の場合はアイコン無しで描画) </summary>
            public Sprite Sprite { get; set; }

            /// <summary> ラベル表示文字 </summary>
            public string Label { get; set; }

            /// <summary> 戻り値として返すユーザーデータ </summary>
            public object UserData { get; set; }
        }

        private const float MinPreviewSize = 60f;
        private const float MaxPreviewSize = 200f;
        private const float DefaultPreviewSize = 100f;
        private const float LabelHeight = 28f;
        private const float ItemPadding = 6f;
        private const float ContentPaddingX = 10f;
        private const float ContentPaddingY = 6f;
        private const float FooterHeight = 28f;

        private static readonly Vector2 DefaultWindowSize = new Vector2(740f, 600f);
        private static readonly Color SelectionOutlineColor = new Color(0.4f, 1f, 0f, 1f);

        //----- field -----

        private Item[] items = new Item[0];

        // 選択中の UserData. 順序を保持するため List.
        private List<object> selection = new List<object>();

        // null = 無制限.
        private int? maxSelectCount = null;

        private string searchText = string.Empty;

        private float previewSize = DefaultPreviewSize;

        private Vector2 scrollPosition = Vector2.zero;

        private bool closed = false;

        private GUIStyle labelStyle = null;

        private Subject<object[]> onConfirm = null;

        private Subject<Unit> onCancel = null;

        private CompositeDisposable disposable = new CompositeDisposable();

        //----- property -----

        //----- method -----

        /// <param name="title"> ウィンドウタイトル </param>
        /// <param name="items"> 選択候補 </param>
        /// <param name="initialSelection"> 初期選択 (UserData 一致で判定) </param>
        /// <param name="maxSelectCount"> 最大選択数 (null = 無制限, 1 = クリック即確定) </param>
        public static SpriteSelectorWindow Open(string title, Item[] items, object[] initialSelection, int? maxSelectCount)
        {
            var window = CreateInstance<SpriteSelectorWindow>();

            window.minSize = DefaultWindowSize;
            window.titleContent = new GUIContent(string.IsNullOrEmpty(title) ? "Select Sprite" : title);

            window.items = items ?? new Item[0];
            window.maxSelectCount = maxSelectCount;
            window.selection = new List<object>();

            if (initialSelection != null)
            {
                foreach (var data in initialSelection)
                {
                    if (data == null){ continue; }

                    if (window.selection.Contains(data)){ continue; }

                    window.selection.Add(data);
                }
            }

            // コンパイル開始時にウィンドウを閉じる.
            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => window.Close())
                .AddTo(window.disposable);

            window.ShowUtility();
            window.Focus();

            return window;
        }

        void OnDestroy()
        {
            DisposeSubjects();

            disposable.Dispose();
        }

        void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle("ProgressBarBack")
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 10,
                };
            }

            DrawToolbar();

            DrawGrid();

            DrawFooter();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(4f);

                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;

                    Repaint();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;

                    Repaint();
                };

                searchText = EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(238f));

                // 複数選択モードのみ Clear ボタン表示.
                if (!maxSelectCount.HasValue || 1 < maxSelectCount.Value)
                {
                    GUILayout.Space(4f);

                    using (new DisableScope(selection.Count == 0))
                    {
                        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                        {
                            selection.Clear();

                            Repaint();
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                // 選択数表示.
                var countText = maxSelectCount.HasValue
                    ? $"{selection.Count} / {maxSelectCount.Value}"
                    : $"{selection.Count}";

                GUILayout.Label($"Selected: {countText}", EditorStyles.miniLabel, GUILayout.Width(110f));

                GUILayout.Space(8f);

                // サムネサイズ.
                GUILayout.Label("Size", EditorStyles.miniLabel, GUILayout.Width(28f));

                EditorGUI.BeginChangeCheck();

                previewSize = GUILayout.HorizontalSlider(previewSize, MinPreviewSize, MaxPreviewSize, GUILayout.Width(120f));

                if (EditorGUI.EndChangeCheck())
                {
                    Repaint();
                }

                GUILayout.Space(8f);
            }
        }

        private void DrawGrid()
        {
            var filteredItems = GetFilteredItems();

            var size = previewSize;
            var padded = size + ItemPadding;
            var slotHeight = padded + LabelHeight;
            var viewWidth = position.width - ContentPaddingX * 2f - 16f;
            var columns = Mathf.Max(1, Mathf.FloorToInt(viewWidth / padded));
            var rows = filteredItems.Length == 0 ? 0 : Mathf.CeilToInt((float)filteredItems.Length / columns);

            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition,
                       GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                scrollPosition = scroll.scrollPosition;

                if (filteredItems.Length == 0)
                {
                    GUILayout.Space(20f);

                    EditorGUILayout.HelpBox("No items.", MessageType.Info);

                    return;
                }

                // スクロール領域のサイズ確保.
                var totalHeight = rows * slotHeight + ContentPaddingY * 2f;

                GUILayoutUtility.GetRect(viewWidth, totalHeight);

                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < columns; col++)
                    {
                        var index = row * columns + col;

                        if (filteredItems.Length <= index){ break; }

                        var item = filteredItems[index];

                        var rect = new Rect(
                            ContentPaddingX + col * padded,
                            ContentPaddingY + row * slotHeight,
                            size,
                            size);

                        DrawItem(rect, item);
                    }
                }
            }
        }

        private void DrawItem(Rect rect, Item item)
        {
            var isSelected = selection.Contains(item.UserData);

            // クリック判定.
            if (GUI.Button(rect, GUIContent.none))
            {
                if (Event.current.button == 0)
                {
                    OnItemClicked(item);
                }
            }

            // 描画.
            if (Event.current.type == EventType.Repaint)
            {
                EditorLayoutTools.DrawTiledTexture(rect, EditorLayoutTools.backdropTexture);

                if (item.Sprite != null)
                {
                    DrawSprite(rect, item.Sprite);
                }

                if (isSelected)
                {
                    EditorLayoutTools.Outline(rect, SelectionOutlineColor);
                }
            }

            // ラベル.
            var labelRect = new Rect(rect.x, rect.y + rect.height, rect.width, LabelHeight);

            var prevBg = GUI.backgroundColor;
            var prevContent = GUI.contentColor;

            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
            GUI.contentColor = new Color(1f, 1f, 1f, 0.85f);

            GUI.Label(labelRect, item.Label ?? string.Empty, labelStyle);

            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
        }

        private void OnItemClicked(Item item)
        {
            if (maxSelectCount.HasValue && maxSelectCount.Value == 1)
            {
                // 単一選択モード: クリックで即確定.
                selection.Clear();

                selection.Add(item.UserData);

                Confirm();

                return;
            }

            ToggleSelection(item);

            Repaint();
        }

        private void ToggleSelection(Item item)
        {
            if (selection.Contains(item.UserData))
            {
                selection.Remove(item.UserData);

                return;
            }

            selection.Add(item.UserData);

            // 上限超えたら古い方から捨てる.
            if (maxSelectCount.HasValue)
            {
                while (maxSelectCount.Value < selection.Count)
                {
                    selection.RemoveAt(0);
                }
            }
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            var tex = sprite.texture;

            if (tex == null){ return; }

            // textureRect はアトラス内のピクセル領域.
            var textureRect = sprite.textureRect;

            if (textureRect.width <= 0f || textureRect.height <= 0f){ return; }

            var uv = new Rect(
                textureRect.x / tex.width,
                textureRect.y / tex.height,
                textureRect.width / tex.width,
                textureRect.height / tex.height);

            // 縦横比維持して中央寄せ.
            var aspect = textureRect.width / textureRect.height;

            var clipRect = rect;

            if (aspect < 1f)
            {
                var padding = rect.width * (1f - aspect) * 0.5f;

                clipRect.xMin += padding;
                clipRect.xMax -= padding;
            }
            else if (1f < aspect)
            {
                var padding = rect.height * (1f - 1f / aspect) * 0.5f;

                clipRect.yMin += padding;
                clipRect.yMax -= padding;
            }

            GUI.DrawTextureWithTexCoords(clipRect, tex, uv);
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(FooterHeight)))
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                {
                    Cancel();
                }

                GUILayout.Space(4f);

                using (new DisableScope(selection.Count == 0))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(80f)))
                    {
                        Confirm();
                    }
                }

                GUILayout.Space(8f);
            }

            GUILayout.Space(4f);
        }

        private void Confirm()
        {
            if (closed){ return; }

            closed = true;

            var result = selection.ToArray();

            if (onConfirm != null)
            {
                onConfirm.OnNext(result);

                onConfirm.OnCompleted();
            }

            Close();
        }

        private void Cancel()
        {
            if (closed){ return; }

            closed = true;

            if (onCancel != null)
            {
                onCancel.OnNext(Unit.Default);

                onCancel.OnCompleted();
            }

            Close();
        }

        private void DisposeSubjects()
        {
            if (onConfirm != null)
            {
                onConfirm.Dispose();
                onConfirm = null;
            }

            if (onCancel != null)
            {
                onCancel.Dispose();
                onCancel = null;
            }
        }

        private Item[] GetFilteredItems()
        {
            if (string.IsNullOrEmpty(searchText)){ return items; }

            var keywords = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return items.Where(x => (x.Label ?? string.Empty).IsMatch(keywords)).ToArray();
        }

        public Observable<object[]> OnConfirmAsObservable()
        {
            return onConfirm ?? (onConfirm = new Subject<object[]>());
        }

        public Observable<Unit> OnCancelAsObservable()
        {
            return onCancel ?? (onCancel = new Subject<Unit>());
        }
    }
}
