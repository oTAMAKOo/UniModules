
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.GameText.Editor
{
    public sealed class BuiltInGameTextWindow : GenerateWindowBase<BuiltInGameTextWindow>
    {
        //----- params -----

        private enum Mode
        {
            BuiltIn,
            AssetBundle,
        }

        public const string WindowTitle = "GameText-BuiltIn";

        //----- field -----

        private Mode mode = Mode.BuiltIn;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            mode = Mode.BuiltIn;

            Show(true);
        }

        void OnGUI()
        {
            var gameText = GameText.Instance;

            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            EditorGUILayout.Separator();

            var builtInGameTextSetting = config.BuiltInGameText;

            var updateGameTextSetting = config.UpdateGameText;

            // 言語情報.
            var info = GetCurrentLanguageInfo();
            
            EditorLayoutTools.DrawContentTitle("Asset");

            using (new ContentsScope())
            {
                GUILayout.Space(4f);

                if (updateGameTextSetting.Enable)
                {
                    var enumValues = Enum.GetValues(typeof(Mode)).Cast<Mode>().ToArray();

                    var index = enumValues.IndexOf(x => x == mode);

                    var tabItems = enumValues.Select(x => x.ToString()).ToArray();

                    EditorGUI.BeginChangeCheck();

                    index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed);

                    if (EditorGUI.EndChangeCheck())
                    {
                        mode = enumValues.ElementAtOrDefault(index);
                    }

                    GUILayout.Space(4f);
                }

                // 生成制御.
                using (new DisableScope(info == null))
                {
                    if (GUILayout.Button("Generate"))
                    {
                        var assetFolderName = gameText.GetAssetFolderName();

                        var assetType = GameText.AssetType.BuiltIn;

                        var aseetFolderPath = string.Empty;

                        var scriptFolderPath = string.Empty;

                        switch (mode)
                        {
                            case Mode.BuiltIn:
                                assetType = GameText.AssetType.BuiltIn;
                                scriptFolderPath = builtInGameTextSetting.ScriptFolderPath;
                                aseetFolderPath = builtInGameTextSetting.AseetFolderPath;
                                break;

                            case Mode.AssetBundle:
                                assetType = GameText.AssetType.Update;
                                aseetFolderPath = updateGameTextSetting.AseetFolderPath;
                                break;
                        }

                        var assetFileName = GameText.GetAssetFileName(assetType, info.Identifier);

                        var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });

                        var contentsFolderPath = builtInGameTextSetting.GetContentsFolderPath();

                        var generateInfo = new GameTextGenerater.GenerateInfo
                        {
                            assetPath = assetPath,
                            contentsFolderPath = contentsFolderPath,
                            scriptFolderPath = scriptFolderPath,
                            textIndex = info.TextIndex,
                        };

                        GameTextGenerater.Generate(generateInfo);

                        Repaint();
                    }

                }
            }

            // エクセル制御.
            ControlExcelGUIContents(builtInGameTextSetting);

            // 言語選択.
            SelectLanguageGUIContents();
        }
    }
}
