
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UniRx;

namespace Modules.Networking
{
    public sealed class ApiHistoryView : TreeView
    {
        //----- params -----

        private sealed class ApiHistoryViewItem : TreeViewItem
        {
            public WebRequestInfo Info { get; private set; }

            public ApiHistoryViewItem(WebRequestInfo info) : base(info.Id)
            {
                Info = info;
            }
        }

        //----- field -----

        private IReadOnlyList<TreeViewItem> currentItems = null;

        private WebRequestInfo[] contentsInfos = null;

        private Dictionary<WebRequestInfo.RequestType, Texture2D> statusLabelTexture = null;
        private Dictionary<WebRequestInfo.RequestStatus, Texture2D> requestStatusTexture = null;

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

            SetColumns();

            LoadTextures();
        }

        private void SetColumns()
        {
            var columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent(string.Empty),
                    width = 20,
                    maxWidth = 20,
                    minWidth = 20,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Type"),
                    width = 60,
                    maxWidth = 60,
                    minWidth = 60,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("API"),
                    width = 200,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("StatusCode"),
                    width = 75,
                    maxWidth = 75,
                    minWidth = 75,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("RetryCount"),
                    width = 80,
                    maxWidth = 80,
                    minWidth = 80,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Time"),
                    width = 80,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                },
            };

            contentsInfos = new WebRequestInfo[0];

            multiColumnHeader = new MultiColumnHeader(new MultiColumnHeaderState(columns));

            multiColumnHeader.ResizeToFit();

            Reload();
        }

        private void LoadTextures()
        {
            statusLabelTexture = new Dictionary<WebRequestInfo.RequestType, Texture2D>()
            {
                { WebRequestInfo.RequestType.Post,   EditorGUIUtility.FindTexture("sv_label_3") },
                { WebRequestInfo.RequestType.Put,    EditorGUIUtility.FindTexture("sv_label_5") },
                { WebRequestInfo.RequestType.Get,    EditorGUIUtility.FindTexture("sv_label_1") },
                { WebRequestInfo.RequestType.Delete, EditorGUIUtility.FindTexture("sv_label_7") },
            };

            requestStatusTexture = new Dictionary<WebRequestInfo.RequestStatus, Texture2D>()
            {
                { WebRequestInfo.RequestStatus.Connection, EditorGUIUtility.IconContent("d_lightRim").image as Texture2D   },
                { WebRequestInfo.RequestStatus.Success,    EditorGUIUtility.IconContent("d_greenLight").image as Texture2D },
                { WebRequestInfo.RequestStatus.Failure,    EditorGUIUtility.IconContent("d_redLight").image as Texture2D   },
                { WebRequestInfo.RequestStatus.Retry,      EditorGUIUtility.IconContent("d_orangeLight").image as Texture2D },
                { WebRequestInfo.RequestStatus.Cancel,     EditorGUIUtility.IconContent("d_lightOff").image as Texture2D   },
            };
        }

        public void SetContents(WebRequestInfo[] contentsInfos)
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

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;

                labelStyle.alignment = TextAnchor.MiddleLeft;

                CenterRectUsingSingleLineHeight(ref rect);
                
                switch (columnIndex)
                {
                    case 0:
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

                    case 1:
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

                    case 2:
                        {
                            var apiName = info.Url.Replace(apiTracker.ServerUrl, string.Empty);

                            EditorGUI.LabelField(rect, apiName, labelStyle);
                        }
                        break;

                    case 3:
                        {
                            EditorGUI.LabelField(rect, info.StatusCode, labelStyle);
                        }
                        break;

                    case 4:
                        {
                            EditorGUI.LabelField(rect, info.RetryCount.ToString(), labelStyle);
                        }
                        break;

                    case 5:
                        {
                            EditorGUI.LabelField(rect, info.Time.ToString(), labelStyle);
                        }
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
