﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Spreadsheet;
using Modules.Devkit.Generators;

namespace Modules.LocalMaster
{
    public abstract class LocalMasterGenerateWindowBase : SpreadsheetConnectionWindow
    {
        //----- params -----

        private class GenerateInfo
        {
            public string AssetFolderPath { get; private set; }
            public string SpreadsheetId { get; private set; }
            public ILocalMasterAssetGenerator Generator { get; private set; }

            public GenerateInfo(string assetFolderPath, string spreadsheetId, ILocalMasterAssetGenerator generator)
            {
                this.AssetFolderPath = assetFolderPath;
                this.SpreadsheetId = spreadsheetId;
                this.Generator = generator;
            }
        }

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;
        private List<GenerateInfo> localMasters = new List<GenerateInfo>();

        //----- property -----
        
        public abstract LocalMasterInfo[] LocalMasterInfos { get; }

        public override string WindowTitle { get { return "LocalMaster"; } }

        public override Vector2 WindowSize { get { return new Vector2(250f, 30f); } }

        //----- method -----    

        protected override void DrawGUI()
        {
            var localMasterConfig = LocalMasterConfig.Instance;

            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            var localMastersByMasterName = LocalMasterInfos.ToLookup(x => x.MasterName);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                var assetFolderPath = localMasterConfig.AssetFolderPath;

                foreach (var item in localMastersByMasterName)
                {
                    if(GUILayout.Button(item.Key))
                    {
                        foreach (var info in item)
                        {
                            var masterGenerator = new LocalMasterAssetGenerator(info.MasterAssetType, info.MasterName);
                            localMasters.Add(new GenerateInfo(assetFolderPath, info.SpreadsheetId, masterGenerator));
                        }
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }

            if(EditorGUI.EndChangeCheck())
            {
                Generate(localMasterConfig);
                Repaint();
            }

            EditorGUI.EndDisabledGroup();
        }

        protected void Generate(LocalMasterConfig localMasterConfig)
        {
            var progressTitle = "Generate Progress";
            var progressMessage = string.Empty;

            var masterNameAddress = localMasterConfig.MasterNameAddress;

            var generateTargets = localMasters.ToLookup(x => x.SpreadsheetId);

            localMasters.Clear();

            foreach (var target in generateTargets)
            {
                var spreadsheetId = target.Key;

                progressMessage = "Load Spreadsheet.";
                EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

                // Spreadsheetに接続しデータを取得 (同期通信).
                var spreadsheets = connector.GetSpreadsheet(spreadsheetId);
                
                EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 1f);
                
                if(spreadsheets.Any())
                {
                    AssetDatabase.StartAssetEditing();

                    foreach (var info in target)
                    {
                        var masterAsset = LoadAsset(info);

                        var asset = masterAsset as LocalMasterAsset;
                        var masterName = info.Generator.MasterName;

                        // 対象シートを取得.
                        var spreadsheet = spreadsheets.FirstOrDefault(x => x.GetValue(masterNameAddress) == masterName);

                        if(spreadsheet == null)
                        {
                            Debug.LogErrorFormat("Spreadsheet: {0} NotFound...", masterName);
                            continue;
                        }

                        // 全更新するので最も最近編集されたシート情報から最終更新日を取得.
                        var spreadsheetsUpdateDate = spreadsheets.Select(x => x.LastUpdateDate).Max().ToUnixTime();

                        var lastUpdateDate = asset.updateTime.HasValue ? asset.updateTime.Value : DateTime.MinValue.ToUnixTime();

                        // ローカルデータが最新なら更新処理は行わない.
                        if (lastUpdateDate < spreadsheetsUpdateDate)
                        {
                            progressMessage = "Generateing LocalMasterAsset.";
                            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0f);

                            info.Generator.Generate(masterAsset, localMasterConfig, spreadsheet);

                            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 0.8f);

                            SaveAsset(masterAsset, spreadsheet.LastUpdateDate.ToUnixTime());
                            
                            EditorUtility.DisplayProgressBar(progressTitle, progressMessage, 1f);

                            Debug.LogFormat("Generate Complete: {0}", masterName);
                        }
                    }

                    AssetDatabase.SaveAssets();

                    AssetDatabase.StopAssetEditing();
                }
                else
                {
                    Debug.LogFormat("Files Latest state.");
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private UnityEngine.Object LoadAsset(GenerateInfo info)
        {
            var assetPath = PathUtility.Combine(info.AssetFolderPath, info.Generator.MasterName + ".asset");

            if (string.IsNullOrEmpty(info.AssetFolderPath)) { return null; }

            var asset = AssetDatabase.LoadAssetAtPath(assetPath, info.Generator.MasterAssetType);

            if (asset == null)
            {
                asset = ScriptableObjectGenerator.Generate(info.Generator.MasterAssetType, assetPath);
            }

            return asset;
        }

        private void SaveAsset(UnityEngine.Object asset, long lastUpdateDate)
        {
            var localMasterAsset = asset as LocalMasterAsset;

            localMasterAsset.SetUpdateTime(lastUpdateDate);
            EditorUtility.SetDirty(asset);
        }
    }
}
 