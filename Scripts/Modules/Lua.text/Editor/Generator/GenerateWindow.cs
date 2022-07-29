
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
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

			minSize = new Vector2(200, 120f);

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

			EditorLayoutTools.Title("Asset");
            
			GUILayout.Space(4f);
            
			// 全生成.
			using (new DisableScope(languageInfo == null))
			{
				if (GUILayout.Button("Generate All"))
				{
					GenerateAll().Forget();
				}
			}

			GUILayout.Space(4f);
		}

		private async UniTask GenerateAll()
		{
			var bookDatas = await DataLoader.LoadBookAllData();

			var count = bookDatas.Length;

			for (var i = 0; i < count; i++)
			{
				var bookData = bookDatas[i];

				EditorUtility.DisplayProgressBar("Generate All", bookData.DestPath, (float)i / count);

				LuaTextAssetGenerator.Generate(bookData);
			}

			EditorUtility.ClearProgressBar();

			UnityConsole.Info("LuaText generate all finish.");

			Repaint();
		}
	}
}