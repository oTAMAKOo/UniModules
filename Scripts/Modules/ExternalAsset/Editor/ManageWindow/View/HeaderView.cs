
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.ExternalAssets
{
    public sealed class HeaderView : LifetimeDisposable
    {
        //----- params -----

        private enum NameEditMode
        {
            None,
            New,
            Rename,
        }

        //----- field -----

        private AssetManagement assetManagement = null;

        private List<string> groupNames = null;

        private string currentGroup = null;

        private string editGroupName = string.Empty;

        private NameEditMode nameEditMode = NameEditMode.None;

        private string searchText = string.Empty;

		private GUIContent iconP4_Updating = null;
		private GUIContent iconToolbarPlus = null;
		private GUIContent iconToolbarMinus = null;
		private GUIContent iconBack = null;

        private Subject<Unit> onChangeSelectGroup = null;

        private Subject<Unit> onRequestRepaint = null;

        private bool initialized = false;

        //----- property -----

        public string Group { get { return currentGroup; } }

        public bool IsGroupNameEdit { get { return nameEditMode != NameEditMode.None; } }

        //----- method -----

        public void Initialize(AssetManagement assetManagement)
        {
            if (initialized) { return; }

            this.assetManagement = assetManagement;

			groupNames = assetManagement.GetAllGroupNames().ToList();

			iconP4_Updating = EditorGUIUtility.IconContent("P4_Updating");
			iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus");
			iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus");
			iconBack = EditorGUIUtility.IconContent("back");

            initialized = true;
        }

        public void DrawGUI()
        {
            var selection = groupNames != null ? groupNames.IndexOf(x => x == currentGroup) : -1;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (nameEditMode != NameEditMode.None)
                {
					if (GUILayout.Button(iconBack, EditorStyles.toolbarButton, GUILayout.Width(28f)))
					{
						nameEditMode = NameEditMode.None;

						editGroupName = null;
					}

					EditorGUI.BeginChangeCheck();

					editGroupName = EditorGUILayout.DelayedTextField(editGroupName);

                    if (EditorGUI.EndChangeCheck())
                    {
						var newGroupName = editGroupName.Trim(' ', '/');

						if (!string.IsNullOrEmpty(editGroupName))
						{
	                        if (newGroupName == ExternalAsset.ShareGroupName)
	                        {
	                            EditorUtility.DisplayDialog("Error", "GroupName is reserved and cannot be used.", "Close");
	                        }
							else if(groupNames.Any(x => x == newGroupName))
							{
								EditorUtility.DisplayDialog("Warning", "GroupName already exists.", "Close");
							}
	                        else
	                        {
	                            switch (nameEditMode)
	                            {
	                                case NameEditMode.New:
	                                    {
											groupNames.Add(newGroupName);

	                                        currentGroup = newGroupName;
	                                    }
	                                    break;

	                                case NameEditMode.Rename:
	                                    {
											if (currentGroup != newGroupName)
	                                        {
												assetManagement.RenameGroup(currentGroup, newGroupName);

												groupNames.AddRange(assetManagement.GetAllGroupNames());

												groupNames = groupNames.Where(x => x != currentGroup).ToList();

												groupNames.Add(newGroupName);

												groupNames = groupNames.Distinct().ToList();

												currentGroup = newGroupName;
											}
										}
	                                    break;
	                            }
	                        }
						}

						nameEditMode = NameEditMode.None;

                        editGroupName = null;

                        if (onChangeSelectGroup != null)
                        {
							onChangeSelectGroup.OnNext(Unit.Default);
                        }

                        if (onRequestRepaint != null)
                        {
                            onRequestRepaint.OnNext(Unit.Default);
                        }
                    }
				}
                else
                {
                    if (currentGroup != ExternalAsset.ShareGroupName)
                    {
                        if (GUILayout.Button(iconToolbarPlus, EditorStyles.toolbarButton, GUILayout.Width(28f)))
                        {
                            nameEditMode = NameEditMode.New;
                        }

                        if (!string.IsNullOrEmpty(currentGroup))
                        {
                            if (GUILayout.Button(iconToolbarMinus, EditorStyles.toolbarButton, GUILayout.Width(28f)))
                            {
                                if (EditorUtility.DisplayDialog("Confirm", "Remove selection group.", "Apply", "Cancel"))
                                {
                                    assetManagement.DeleteGroup(currentGroup);

                                    groupNames = assetManagement.GetAllGroupNames().ToList();

                                    if (onChangeSelectGroup != null)
                                    {
										onChangeSelectGroup.OnNext(Unit.Default);
                                    }

                                    if (onRequestRepaint != null)
                                    {
                                        onRequestRepaint.OnNext(Unit.Default);
                                    }

                                    return;
                                }
                            }

                            if (GUILayout.Button(iconP4_Updating, EditorStyles.toolbarButton, GUILayout.Width(28f)))
                            {
                                nameEditMode = NameEditMode.Rename;

                                editGroupName = currentGroup;

                                if (onRequestRepaint != null)
                                {
                                    onRequestRepaint.OnNext(Unit.Default);
                                }

                                return;
                            }
                        }
                    }

					// グループ選択.

                    EditorGUI.BeginChangeCheck();

                    var groupName = groupNames.ElementAtOrDefault(selection);

                    var displayGroupNames = GetDisplayGroupNames();

					displayGroupNames = displayGroupNames.OrderBy(x => x, new NaturalComparer()).ToList();

                    var index = displayGroupNames.IndexOf(x => x == groupName);

                    var displayLabels = displayGroupNames.Select(x => ConvertSlashToUnicodeSlash(x)).ToArray();

                    index = EditorGUILayout.Popup(string.Empty, index, displayLabels, EditorStyles.toolbarDropDown);

                    if (EditorGUI.EndChangeCheck())
                    {
                        selection = groupNames.IndexOf(x => x == displayGroupNames[index]);

                        currentGroup = groupNames[selection];

                        if (onChangeSelectGroup != null)
                        {
							onChangeSelectGroup.OnNext(Unit.Default);
                        }
                    }

					GUILayout.Space(4f);

					// 検索.

                    Action<string> onChangeSearchText = x =>
                    {
                        searchText = x;
                    };

                    Action onSearchCancel = () =>
                    {
                        searchText = string.Empty;
                    };

                    EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MaxWidth(250f));
              
                }
            }
        }
        
        private List<string> GetDisplayGroupNames()
        {
            // 検索テキストでフィルタ.

            if (string.IsNullOrEmpty(searchText)) { return groupNames; }
            
            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return groupNames.Where(x => x.IsMatch(keywords)).ToList();
        }

        private string ConvertSlashToUnicodeSlash(string text)
        {
            return text.Replace('/', '\u2215');
        }

        public IObservable<Unit> OnChangeSelectGroupAsObservable()
        {
            return onChangeSelectGroup ?? (onChangeSelectGroup = new Subject<Unit>());
        }

        public IObservable<Unit> OnRequestRepaintAsObservable()
        {
            return onRequestRepaint ?? (onRequestRepaint = new Subject<Unit>());
        }
    }
}
