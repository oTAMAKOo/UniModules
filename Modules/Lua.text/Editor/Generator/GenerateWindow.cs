
#if ENABLE_XLUA

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;

namespace Modules.Lua.Text
{
    public sealed class GenerateWindow : SingletonEditorWindow<GenerateWindow>
    {
        //----- params -----

		public const string WindowTitle = "LuaText Generate";

        //----- field -----

		//----- property -----

        //----- method -----

		public static void Open()
		{
			Instance.Initialize();
		}

		private void Initialize()
		{
			titleContent = new GUIContent(WindowTitle);

			minSize = new Vector2(200, 100f);

			Show(true);
		}

		void OnGUI()
		{
			var languageManager = LuaTextLanguage.Instance;

			var generateInfos = languageManager.Current;

			if (generateInfos != null)
			{
				EditorGUILayout.Separator();

				// 生成.
				DrawGenerateGUI();
			}
			else
			{
				EditorGUILayout.HelpBox("No language selected.\nPlease select language.", MessageType.Error);
			}
		}

		private void DrawGenerateGUI()
		{
			var languageManager = LuaTextLanguage.Instance;

			var languageInfo = languageManager.Current;

			GUILayout.Space(4f);

            using (new DisableScope(languageInfo == null))
			{
                if (GUILayout.Button("Import All"))
                {
                    ImportAll().Forget();
                }

                GUILayout.Space(4f);

                if (GUILayout.Button("Export All"))
                {
                    ExportAll().Forget();
                }

                GUILayout.Space(4f);

				if (GUILayout.Button("Generate All"))
				{
					GenerateAll().Forget();
				}
			}

			GUILayout.Space(4f);
		}

        private async UniTask ImportAll()
        {
            var config = LuaTextConfig.Instance;

            var transferInfos = config.TransferInfos.ToArray();

            var count = transferInfos.Length;

            for (var i = 0; i < count; i++)
            {
                var transferInfo = transferInfos[i];

                var directory = UnityPathUtility.RelativePathToFullPath(transferInfo.sourceFolderRelativePath);

                var excelPaths = LuaTextExcel.FindExcelFile(directory);

                EditorUtility.DisplayProgressBar("Import All", directory, (float)i / count);

                if (excelPaths.Any())
                {
                    await LuaTextExcel.Import(directory, excelPaths, true);
                }
            }

            EditorUtility.ClearProgressBar();

            UnityConsole.Info("LuaText import all finish.");

            Repaint();
        }

        private async UniTask ExportAll()
        {
            var config = LuaTextConfig.Instance;

            var transferInfos = config.TransferInfos.ToArray();

            var count = transferInfos.Length;

            for (var i = 0; i < count; i++)
            {
                var transferInfo = transferInfos[i];

                var directory = UnityPathUtility.RelativePathToFullPath(transferInfo.sourceFolderRelativePath);

                var excelPaths = LuaTextExcel.FindExcelFile(directory);

                EditorUtility.DisplayProgressBar("Export All", directory, (float)i / count);

                if (excelPaths.Any())
                {
                    await LuaTextExcel.Export(directory, excelPaths, true);
                }
            }

            EditorUtility.ClearProgressBar();

            UnityConsole.Info("LuaText export all finish.");

            Repaint();
        }

		private async UniTask GenerateAll()
		{
			var bookDatas = await DataLoader.GetAllBookData();

			var tasks = new List<UniTask>();

			var count = bookDatas.Length;

			for (var i = 0; i < count; i++)
			{
				var bookData = bookDatas[i];

				if (LuaTextAssetGenerator.IsRequireUpdate(bookData))
				{
					var task = UniTask.Create(async () =>
					{
						var sheetDatas = await DataLoader.LoadSheetData(bookData);
						
						LuaTextAssetGenerator.Generate(bookData, sheetDatas);
					});

					tasks.Add(task);
				}
			}

			using (new AssetEditingScope())
			{
				await UniTask.WhenAll(tasks);
			}

			EditorUtility.ClearProgressBar();

			UnityConsole.Info("LuaText generate all finish.");

			Repaint();
		}
	}
}

#endif
