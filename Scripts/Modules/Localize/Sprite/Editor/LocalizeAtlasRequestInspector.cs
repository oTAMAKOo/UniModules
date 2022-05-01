
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Localize
{
	[CustomEditor(typeof(LocalizeAtlasRequest))]
    public sealed class LocalizeAtlasRequestInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

		private Vector2 scrollPosition = Vector2.zero;

		private GUIContent toolbarPlusIcon = null;
		private GUIContent toolbarMinusIcon = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }

			toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
			toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");

			initialized = true;
		}

		public override void OnInspectorGUI()
		{
			var instance = target as LocalizeAtlasRequest;

			Initialize();
			
			var update = false;

			var folderGuids = instance.FolderGuids.ToList();

			var removeTarget = new List<string>();

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(8f);

				if (GUILayout.Button(toolbarPlusIcon, GUILayout.Width(30f), GUILayout.Height(16f)))
				{
					folderGuids.Add(null);

					update = true;
				}
			}

			var scrollViewLayoutOption = new List<GUILayoutOption>();

			if (5 < folderGuids.Count)
			{
				scrollViewLayoutOption.Add(GUILayout.MaxHeight(120f));
			}
			
			using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, scrollViewLayoutOption.ToArray()))
            {
                for (var i = 0; i < folderGuids.Count; i++)
                {
                    var folderGuid = folderGuids[i];

					UnityEngine.Object folder = null;

					if (!string.IsNullOrEmpty(folderGuid))
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(folderGuid);

						folder = AssetDatabase.LoadMainAssetAtPath(assetPath);
					}

					using (new EditorGUILayout.HorizontalScope())
					{
						var layoutOption = new GUILayoutOption[]
						{
							GUILayout.Height(16f), GUILayout.ExpandWidth(true),
						};

						EditorGUI.BeginChangeCheck();
	                        
						folder = EditorGUILayout.ObjectField(folder, typeof(UnityEngine.Object), false, layoutOption);
	                    
						if (EditorGUI.EndChangeCheck())
						{
							// フォルダだけ登録可能.
							if (UnityEditorUtility.IsFolder(folder) || folder == null)
							{
								folderGuids[i] = folder == null ? string.Empty : UnityEditorUtility.GetAssetGUID(folder);

								update = true;
							}
						}

						if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(25f)))
						{
							removeTarget.Add(folderGuid);
						}
					}

					GUILayout.Space(2f);
				}

                scrollPosition = scrollView.scrollPosition;
            }

			if (removeTarget.Any())
			{
				foreach (var info in removeTarget)
				{
					folderGuids.Remove(info);
				}

				update = true;
			}

			if (update)
			{
				Reflection.SetPrivateField(instance, "folderGuids", folderGuids.ToArray());
			}
		}
    }
}