
using Extensions;
using UnityEngine;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.GameText.Editor
{
    public sealed class ExtendGameTextWindow : GenerateWindowBase<ExtendGameTextWindow>
    {
        //----- params -----

        public const string WindowTitle = "GameText-Extend";

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

            Show(true);
        }

        void OnGUI()
        {
            var gameText = GameText.Instance;

            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            var extendGameTextSetting = config.ExtendGameText;
            
            // 言語情報.
            var info = GetCurrentLanguageInfo();

            GUILayout.Space(2f);

            EditorLayoutTools.DrawContentTitle("Asset");

            using (new ContentsScope())
            {
                GUILayout.Space(4f);

                // 生成制御.
                using (new DisableScope(info == null))
                {
                    if (GUILayout.Button("Generate"))
                    {
                        var assetFolderName = gameText.GetAssetFolderName();

                        var assetFileName = GameText.GetAssetFileName(GameText.AssetType.Extend, info.Identifier);

                        var assetPath = PathUtility.Combine(new string[] { extendGameTextSetting.AseetFolderPath, assetFolderName, assetFileName });

                        var generateInfo = new GameTextGenerater.GenerateInfo
                        {
                            assetPath = assetPath,
                            contentsFolderPath = extendGameTextSetting.GetContentsFolderPath(),
                            scriptFolderPath = string.Empty,
                            textIndex = info.TextIndex,
                        };

                        GameTextGenerater.Generate(generateInfo);

                        Repaint();
                    }
                }

                GUILayout.Space(2f);
            }

            // エクセル制御.
            ControlExcelGUIContents(extendGameTextSetting);

            // 言語選択.
            SelectLanguageGUIContents();
        }
    }
}
