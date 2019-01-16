
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UniRx;
using Extensions;
using Modules.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Spreadsheet;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public class GameTextGenerateInfo
    {
        public string Language { get; private set; }
        public string FileName { get; private set; }
        public int TextColumn { get; private set; }

        public GameTextGenerateInfo(string language, string fileName, int textColumn)
        {
            Language = language;
            FileName = fileName;
            TextColumn = textColumn;
        }
    }

    public static class GameTextGenerater
    {
        public static void Generate(SpreadsheetConnector connector, GameTextGenerateInfo generateInfo)
        {
            if (generateInfo == null) { return; }

            var progressTitle = "Generate Progress";
            var progressMessage = string.Empty;

            var gameTextConfig = GameTextConfig.Instance;

            progressMessage = "Connection Spreadsheet.";
            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

            var asset = LoadAsset(gameTextConfig.ScriptableObjectFolderPath, generateInfo.FileName);

            progressMessage = "Load GameText form Spreadsheet.";
            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

            // Spreadsheetに接続しデータを取得 (同期通信).
            var spreadsheets = connector.GetSpreadsheet(gameTextConfig.SpreadsheetId).ToArray();

            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 1f);

            if (spreadsheets.Any())
            {
                // 全更新するので最も最近編集されたシート情報から最終更新日を取得.
                var spreadsheetsUpdateDate = spreadsheets.Select(x => x.LastUpdateDate).Max().ToUnixTime();

                var lastUpdateDate = asset.updateTime.HasValue ? asset.updateTime.Value : DateTime.MinValue.ToUnixTime();

                // ローカルデータが最新なら更新処理は行わない.
                if (lastUpdateDate < spreadsheetsUpdateDate)
                {
                    AssetDatabase.StartAssetEditing();

                    progressMessage = "Generating GameTextScript.";
                    EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

                    GameTextScriptGenerator.Generate(spreadsheets, gameTextConfig, generateInfo.TextColumn);

                    EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0.2f);

                    progressMessage = "Generating GameTextAsset.";
                    EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0.5f);

                    GameTextAssetGenerator.Build(asset, spreadsheets, gameTextConfig, generateInfo.TextColumn);

                    EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0.8f);

                    EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 1f);

                    UnityConsole.Info("GameTextを出力しました");

                    SaveLastUpdateDate(asset, spreadsheetsUpdateDate);

                    AssetDatabase.SaveAssets();

                    AssetDatabase.StopAssetEditing();
                }
                else
                {
                    UnityConsole.Info("GameTextは最新の状態です");
                }
            }
            else
            {
                Debug.LogError("Spreadsheetにデータがありません.");
            }

            EditorUtility.ClearProgressBar();
        }

        private static GameTextAsset LoadAsset(string assetFolderPath, string assetName)
        {
            var assetPath = PathUtility.Combine(assetFolderPath, assetName + ".asset");

            if (string.IsNullOrEmpty(assetFolderPath)) { return null; }

            var asset = AssetDatabase.LoadAssetAtPath<GameTextAsset>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObjectGenerator.Generate<GameTextAsset>(assetPath);
            }

            return asset;
        }

        private static void SaveLastUpdateDate(GameTextAsset asset, long lastUpdateDate)
        {
            asset.updateTime = lastUpdateDate;
            EditorUtility.SetDirty(asset);
        }
    }
}
