
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
            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            var builtInGameTextSetting = config.BuiltInGameText;

            var updateGameTextSetting = config.UpdateGameText;

            // 言語情報.
            var info = GetCurrentLanguageInfo();

            GUILayout.Space(2f);

            EditorLayoutTools.ContentTitle("Asset");

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
                        var assetType = GameText.AssetType.BuiltIn;

                        switch (mode)
                        {
                            case Mode.BuiltIn:
                                assetType = GameText.AssetType.BuiltIn;
                                break;

                            case Mode.AssetBundle:
                                assetType = GameText.AssetType.Update;
                                break;
                        }

                        GameTextGenerator.Generate(assetType, info);

                        Repaint();
                    }

                    GUILayout.Space(2f);
                }
            }

            // エクセル制御.
            ControlExcelGUIContents(builtInGameTextSetting);

            // 言語選択.
            SelectLanguageGUIContents();
        }
    }
}
