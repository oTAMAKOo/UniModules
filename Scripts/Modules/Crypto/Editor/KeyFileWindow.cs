
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Crypto
{
    public abstract class KeyFileWindow<TInstance, TKeyType> : SingletonEditorWindow<TInstance> 
		where TInstance : KeyFileWindow<TInstance, TKeyType>
		where TKeyType : Enum
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(580f, 500f);

		private sealed class KeyInfo
		{
			public TKeyType KeyType { get; set; } 
			public string FileName { get; set; } 
			public string Key { get; set; } 
			public string Iv { get; set; } 
		}

		//----- field -----

		private List<KeyInfo> keyInfos = null;

		private Vector2 scrollPosition = Vector2.zero;
		
        private IKeyFileManager<TKeyType> keyFileManager = null;

        //----- property -----

        //----- method -----

        public static void Open(IKeyFileManager<TKeyType> keyFileManager)
        {
            Instance.minSize = WindowSize;
			Instance.maxSize = WindowSize;

            Instance.titleContent = new GUIContent("Generate KeyFile");

            Instance.keyFileManager = keyFileManager;

			Instance.LoadKeyInfo();

			Instance.ShowUtility();
        }

        void OnGUI()
        {
			using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
			{
				foreach (var keyInfo in keyInfos)
				{
					using (new ContentsScope())
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							EditorGUILayout.SelectableLabel("KeyType : " + keyInfo.KeyType, GUILayout.Height(EditorGUIUtility.singleLineHeight));

							GUILayout.FlexibleSpace();

							using (new DisableScope(keyInfo.Key.IsNullOrEmpty() || keyInfo.Iv.IsNullOrEmpty()))
							{
								if (GUILayout.Button("Generate", EditorStyles.miniButton, GUILayout.Width(80f)))
								{
									var fileName = keyFileManager.GetFileName(keyInfo.KeyType);

									var filePath = PathUtility.Combine(keyFileManager.FileDirectory, fileName);

									keyFileManager.Create(filePath, keyInfo.Key, keyInfo.Iv);

									var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(filePath);

									AssetDatabase.ImportAsset(assetPath);
								}
							}
						}

						EditorGUILayout.SelectableLabel("FileName : " + keyInfo.FileName, GUILayout.Height(EditorGUIUtility.singleLineHeight));

						using (new LabelWidthScope(50f))
						{
							var key = EditorGUILayout.TextField("Key", keyInfo.Key);

							if (EditorGUI.EndChangeCheck())
							{
								if (!key.IsNullOrEmpty())
								{
									if (key.Length == 32)
									{
										keyInfo.Key = key;
									}
									else
									{
										Debug.LogError("Key must be 32 characters");
									}
								}
							}

							GUILayout.Space(2f);
	                
							EditorGUI.BeginChangeCheck();

							var iv = EditorGUILayout.TextField("Iv", keyInfo.Iv);

							if (EditorGUI.EndChangeCheck())
							{
								if (!iv.IsNullOrEmpty())
								{
									if (iv.Length == 16)
									{
										keyInfo.Iv = iv;
									}
									else
									{
										Debug.LogError("Iv must be 16 characters");
									}
								}
							}
						}
					}
				}

				scrollPosition = scrollViewScope.scrollPosition;
			}
		}

		private void LoadKeyInfo()
		{
			keyInfos = new List<KeyInfo>();

			keyFileManager.ClearCache();

			var enumNames = Enum.GetNames(typeof(TKeyType));

			foreach (var enumName in enumNames)
			{
				var enumValue = EnumExtensions.FindByName(enumName , default(TKeyType));
				
				var fileName = keyFileManager.GetFileName(enumValue);

				var keyData = keyFileManager.Get(enumValue);
				
				var keyInfo = new KeyInfo()
				{
					KeyType = enumValue,
					FileName = fileName,
					Key = keyData != null ? keyData.Key : null,
					Iv = keyData != null ? keyData.Iv : null,
				};

				keyInfos.Add(keyInfo);
			}
		}
	}
}
