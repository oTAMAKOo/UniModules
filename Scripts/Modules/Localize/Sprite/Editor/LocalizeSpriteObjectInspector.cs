
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;

namespace Modules.Localize
{
	[CustomEditor(typeof(LocalizeSpriteAsset))]
    public sealed class LocalizeSpriteObjectInspector : UnityEditor.Editor
    {
        //----- params -----

		//----- field -----

		private bool isChanged = false;

		private Vector2 scrollPosition = Vector2.zero;

		private GUIContent toolbarPlusIcon = null;
		private GUIContent toolbarMinusIcon = null;

		private GUIStyle inputFieldStyle = null;

		private AesCryptoKey cryptoKey = null;

		private List<LocalizeSpriteAsset.FolderInfo> folderInfos = null;

		[NonSerialized]
		private bool initialized = false;

        //----- property -----

        //----- method -----

		private void Initialize()
		{
			if (initialized){ return; }

			var instance = target as LocalizeSpriteAsset;

			cryptoKey = ProjectCryptoKey.Instance.GetCryptoKey();

			toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
			toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");

			// データを直接編集しないようにコピーを生成.
			folderInfos = instance.Infos
				.Select(x => x.DeepCopy())
				.ToList();

			foreach (var folderInfo in folderInfos)
			{
				try
				{
					folderInfo.description = folderInfo.description.Decrypt(cryptoKey);
				}
				catch (Exception e)
				{
					Debug.LogException(e);

					folderInfo.description = string.Empty;
				}
			}

			initialized = true;
		}

		private void InitializeStyle()
		{
			if (inputFieldStyle == null)
			{
				inputFieldStyle = new GUIStyle(EditorStyles.miniTextField);
			}
		}

		void OnEnable()
		{
			isChanged = false;
		}

		void OnDisable()
		{
			if (isChanged)
			{
				UnityEditorUtility.SaveAsset(target);
			}
		}

		public override void OnInspectorGUI()
        {
            Initialize();

            InitializeStyle();

            var instance = target as LocalizeSpriteAsset;

			var removeInfos = new List<LocalizeSpriteAsset.FolderInfo>();

            var update = false;

            GUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(toolbarPlusIcon, GUILayout.Width(50f), GUILayout.Height(16f)))
                {
                    var info = new LocalizeSpriteAsset.FolderInfo();

					folderInfos.Add(info);

                    update = true;
                }

                GUILayout.Space(8f);
            }

            var swapTargets = new List<Tuple<int, int>>();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandWidth(true)))
            {
                for (var i = 0; i < folderInfos.Count; i++)
                {
                    var info = folderInfos[i];

                    using (new ContentsScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();

                            var index = EditorGUILayout.DelayedIntField("Index", i, inputFieldStyle, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (0 <= index && index < folderInfos.Count)
                                {
                                    swapTargets.Add(Tuple.Create(i, index));
                                }
                            }

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(35f)))
                            {
                                removeInfos.Add(info);
                            }
                        }

                        GUILayout.Space(2f);

						UnityEngine.Object folder = null;

						if (!string.IsNullOrEmpty(info.guid))
						{
							var assetPath = AssetDatabase.GUIDToAssetPath(info.guid);

							folder = AssetDatabase.LoadMainAssetAtPath(assetPath);
						}

						var layoutOption = new GUILayoutOption[]
						{
							GUILayout.Height(16f), GUILayout.ExpandWidth(true),
						};

                        EditorGUI.BeginChangeCheck();
                        
						folder = EditorGUILayout.ObjectField("Atlas Folder", folder, typeof(UnityEngine.Object), false, layoutOption);
                        
                        GUILayout.Space(2f);

						info.description = EditorGUILayout.DelayedTextField("Description", info.description, inputFieldStyle, layoutOption);

                        if (EditorGUI.EndChangeCheck())
                        {
							// フォルダだけ登録可能.
							if (UnityEditorUtility.IsFolder(folder) || folder == null)
							{
								info.guid = folder == null ? string.Empty : UnityEditorUtility.GetAssetGUID(folder);
							}

							update = true;
                        }
                    }

                    GUILayout.Space(4f);
                }

                scrollPosition = scrollView.scrollPosition;
            }

            if (swapTargets.Any())
            {
                foreach (var swapTarget in swapTargets)
                {
					folderInfos = folderInfos.Swap(swapTarget.Item1, swapTarget.Item2).ToList();
                }

                update = true;
            }

            if (removeInfos.Any())
            {
                foreach (var info in removeInfos)
                {
					folderInfos.Remove(info);
                }

                update = true;
            }

            if (update)
            {
				var infos = folderInfos
					.Select(x =>
						{
							var info = x.DeepCopy();
							info.description = info.description.Encrypt(cryptoKey);
							return info;
						})
					.ToArray();

				Reflection.SetPrivateField(instance, "infos", infos);

                isChanged = true;
            }
        }
    }
}