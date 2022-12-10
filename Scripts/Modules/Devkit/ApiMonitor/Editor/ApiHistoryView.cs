
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UniRx;

namespace Modules.Net.WebRequest
{
    public sealed class ApiHistoryView : TreeView
    {
        //----- params -----

        private enum Column
        {
            Status,
            Type,
            Api,
            StatusCode,
            ElapsedTime,
            RetryCount,
            StartTime,
            FinishTime,
        }

        private sealed class ColumnInfo
        {
            public string Label { get; private set; }

            public float Width { get; private set; }
            
            public bool FixedWidth  { get; private set; }

            public ColumnInfo(string label, float width, bool fixedWidth = true)
            {
                Label = label;
                Width = width;
                FixedWidth = fixedWidth;
            }
        }

        private sealed class ApiHistoryViewItem : TreeViewItem
        {
            public ApiInfo Info { get; private set; }

            public ApiHistoryViewItem(ApiInfo info) : base(info.Id)
            {
                Info = info;
            }
        }

        //----- field -----

        private IReadOnlyList<TreeViewItem> currentItems = null;

        private ApiInfo[] contentsInfos = null;

        private Dictionary<ApiInfo.RequestType, Texture2D> statusLabelTexture = null;
        private Dictionary<ApiInfo.RequestStatus, Texture2D> requestStatusTexture = null;

        private GUIStyle statusLabelStyle = null;
        private GUIStyle requestStatusStyle = null;
        private GUIStyle apiNameLabelStyle = null;

        private Subject<int> onChangeSelect = null;

        //----- property -----

        //----- method -----

        public ApiHistoryView() : base(new TreeViewState())
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            contentsInfos = new ApiInfo[0];
            
            SetColumns();

            LoadTextures();
        }

        private void SetColumns()
        {
            var columnCount = Enum.GetValues(typeof(Column)).Length;

            var columns = new MultiColumnHeaderState.Column[columnCount];

            var columnTable = new Dictionary<Column, ColumnInfo>()
            {
                { Column.Status,      new ColumnInfo(string.Empty, 20) },
                { Column.Type,        new ColumnInfo("Type", 60) },
                { Column.Api,         new ColumnInfo("API", 200, false) },
                { Column.StatusCode,  new ColumnInfo("StatusCode", 75) },
                { Column.RetryCount,  new ColumnInfo("RetryCount", 80) },
                { Column.ElapsedTime, new ColumnInfo("Time", 80) },
                { Column.StartTime,   new ColumnInfo("StartTime", 100) },
                { Column.FinishTime,  new ColumnInfo("FinishTime", 100) },
            };

            foreach (var item in columnTable)
            {
                var column = new MultiColumnHeaderState.Column();

                var info = item.Value;

                column.headerContent = new GUIContent(info.Label);
                column.width = info.Width;
                column.headerTextAlignment = TextAlignment.Center;
                column.canSort = false;
                column.autoResize = false;

                if (info.FixedWidth)
                {
                    column.minWidth = info.Width;
                    column.maxWidth = info.Width;
                }

                columns[(int)item.Key] = column;
            }

            var columnHeader = new ColumnHeader(new MultiColumnHeaderState(columns));

            columnHeader.Initialize();

            multiColumnHeader = columnHeader;

            Reload();
        }

        private void LoadTextures()
        {
            statusLabelTexture = new Dictionary<ApiInfo.RequestType, Texture2D>()
            {
                { ApiInfo.RequestType.Post,   EditorGUIUtility.FindTexture("sv_label_3") },
                { ApiInfo.RequestType.Put,    EditorGUIUtility.FindTexture("sv_label_5") },
                { ApiInfo.RequestType.Get,    EditorGUIUtility.FindTexture("sv_label_1") },
                { ApiInfo.RequestType.Delete, EditorGUIUtility.FindTexture("sv_label_7") },
            };

            requestStatusTexture = new Dictionary<ApiInfo.RequestStatus, Texture2D>()
            {
                { ApiInfo.RequestStatus.Connection, EditorGUIUtility.IconContent("d_lightRim").image as Texture2D   },
                { ApiInfo.RequestStatus.Success,    EditorGUIUtility.IconContent("d_greenLight").image as Texture2D },
                { ApiInfo.RequestStatus.Failure,    EditorGUIUtility.IconContent("d_redLight").image as Texture2D   },
                { ApiInfo.RequestStatus.Retry,      EditorGUIUtility.IconContent("d_orangeLight").image as Texture2D },
                { ApiInfo.RequestStatus.Cancel,     EditorGUIUtility.IconContent("d_lightOff").image as Texture2D   },
            };
        }

        public void SetContents(ApiInfo[] contentsInfos)
        {
            var currentSelected = state.selectedIDs;

            this.contentsInfos = contentsInfos;

            Reload();

            state.selectedIDs = currentSelected;
        }

        private void InitializeStyle()
        {
            if (statusLabelStyle == null)
            {
                statusLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    border = new RectOffset(10, 10, 4, 4),
                    fontSize = 9,
                    fontStyle = FontStyle.Bold,
                    fixedWidth = 46,
                    fixedHeight = 16,
                };

                statusLabelStyle.normal.textColor = Color.white;
            }

            if (apiNameLabelStyle == null)
            {
                apiNameLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                };

                statusLabelStyle.normal.textColor = Color.white;
            }

            if (requestStatusStyle == null)
            {
                requestStatusStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fixedWidth = 43 * 0.25f,
                    fixedHeight = 43 * 0.25f,
                };

                requestStatusStyle.normal.textColor = Color.clear;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { depth = -1 };

            var items = new List<TreeViewItem>();
            
            for (var i = 0; i < contentsInfos.Length; i++)
            {
                if (contentsInfos[i] == null){ continue; }

                var item = new ApiHistoryViewItem(contentsInfos[i]);

                items.Add(item);
            }

            currentItems = items;

            root.children = currentItems as List<TreeViewItem>;

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (onChangeSelect != null)
            {
                var selectedId = selectedIds.IsEmpty() ? -1 : selectedIds.First();

                onChangeSelect.OnNext(selectedId);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var apiTracker = ApiTracker.Instance;

            InitializeStyle();

            var item = args.item as ApiHistoryViewItem;

            var info = item.Info;

            var columns = Enum.GetValues(typeof(Column)).Cast<Column>().ToArray();

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;

                labelStyle.alignment = TextAnchor.MiddleLeft;

                CenterRectUsingSingleLineHeight(ref rect);

                var column = columns.ElementAt(columnIndex);

                switch (column)
                {
                    case Column.Status:
                        {
                            var texture = requestStatusTexture.GetValueOrDefault(info.Status);

                            if (texture != null)
                            {
                                requestStatusStyle.normal.background = texture;
                            }
                            
                            rect.position += new Vector2(4f, 4f);

                            EditorGUI.LabelField(rect, string.Empty, requestStatusStyle);
                        }
                        break;

                    case Column.Type:
                        {
                            var texture = statusLabelTexture.GetValueOrDefault(info.Request);

                            if (texture != null)
                            {
                                statusLabelStyle.normal.background = texture;
                            }

                            var requestName = info.Request.ToLabelName();

                            rect.position += new Vector2(4f, 2f);

                            EditorGUI.LabelField(rect, requestName, statusLabelStyle);
                        }
                        break;

                    case Column.Api:
                        {
                            var apiName = info.Url.Replace(apiTracker.ServerUrl, string.Empty);

                            EditorGUI.LabelField(rect, apiName, labelStyle);
                        }
                        break;

                    case Column.StatusCode:
                        EditorGUI.LabelField(rect, info.StatusCode.IsNullOrEmpty() ? "---" : info.StatusCode, labelStyle);
                        break;

                    case Column.ElapsedTime:
                        EditorGUI.LabelField(rect, info.ElapsedTime.HasValue ? info.ElapsedTime.Value.ToString("F2") : "---", labelStyle);
                        break;

                    case Column.RetryCount:
                        EditorGUI.LabelField(rect, info.RetryCount.ToString(), labelStyle);
                        break;

                    case Column.StartTime:
                        EditorGUI.LabelField(rect, info.Start.HasValue ? info.Start.ToString() : "---", labelStyle);
                        break;

                    case Column.FinishTime:
                        EditorGUI.LabelField(rect, info.Finish.HasValue ? info.Finish.ToString() : "---", labelStyle);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }

        public IObservable<int> OnChangeSelectAsObservable()
        {
            return onChangeSelect ?? (onChangeSelect = new Subject<int>());
        }
    }
}
