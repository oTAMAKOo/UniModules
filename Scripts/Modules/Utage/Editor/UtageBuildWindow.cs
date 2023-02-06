
#if ENABLE_UTAGE

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Utage;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.UtageExtension
{
    public sealed class UtageBuildWindow : SingletonEditorWindow<UtageBuildWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(200f, 65f);
        
        private const string ProjectAssetExtension = ".project.asset";

        private const string ScenarioAssetExtension = ".scenario" + AdvExcelImporter.ScenarioAssetExt;

        private const string ExcelExtension = ".xls";

        private sealed class UtageContent
        {
            public string excelFilePath = null;

            public DateTime? excelLastWriteTime = null;

            public string bookFilePath = null;

            public DateTime? bookFileLastWriteTime = null;
        }

        //----- field -----

        private string excelFolderAssetPath = null;
        private string exportFolderAssetPath = null;

        private AdvScenarioDataProject projectTemplate = null;
        private AdvImportScenarios importScenarioTemplate = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----
		
        public static void Open()
        {
            Instance.Initialize();
            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized){ return; }

            // Window設定.

            titleContent = new GUIContent("Build Utage Script");

            minSize = WindowSize;

            // 初期化.

            var config = UtageBuildConfig.Instance;

            excelFolderAssetPath = config.ExcelFolderAssetPath;
            exportFolderAssetPath = config.ExportFolderAssetPath;

            projectTemplate = config.ScenarioProjectTemplate;
            importScenarioTemplate = config.ImportScenarioTemplate;

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            EditorGUILayout.Separator();

            if (GUILayout.Button("Convert"))
            {
                Convert(false);
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Convert All"))
            {
                Convert(true);
            }
        }

        private void Convert(bool force)
        {
            var check = true;

            if (force)
            {
                check = EditorUtility.DisplayDialog("Confirmation", "Force update all scenario files?", "OK", "Cancel");
            }

            if (!check){ return; }
            
            var targets = GetUtageContents().Where(x => IsRequireUpdate(x, force)).ToArray();

            try
            {
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];

                    var excelAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(target.excelFilePath);

                    EditorUtility.DisplayProgressBar("progress", excelAssetPath, (float)i / targets.Length);

                    Export(target);
                }

                EditorUtility.ClearProgressBar();

                if (targets.Any())
                {
                    AssetDatabase.Refresh();
                }
            }
            catch
            {
                EditorUtility.ClearProgressBar();

                throw;
            }
        }

        private void Export(UtageContent content)
        {
            var project = GetScenarioDataProject(content);
            var importScenarios = GetImportScenarios(content);

            Reflection.SetPrivateField(project, "scenarios", importScenarios);

            SetChapterData(project, content.excelFilePath);

            var originIsEditorErrorCheck = AssetFileManager.IsEditorErrorCheck;

            AssetFileManager.IsEditorErrorCheck = true;
                
            Import(project);

            AssetFileManager.IsEditorErrorCheck = originIsEditorErrorCheck;

            MoveScenarioAsset(content);
        }

        private AdvScenarioDataProject GetScenarioDataProject(UtageContent content)
        {
            var projectFilePath = content.excelFilePath.Replace(ExcelExtension, ProjectAssetExtension);
            var projectAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(projectFilePath);

            var projectTemplateAssetPath = AssetDatabase.GetAssetPath(projectTemplate);
            var projectTemplateFullPath = UnityPathUtility.ConvertAssetPathToFullPath(projectTemplateAssetPath);

            if (!File.Exists(projectFilePath))
            { 
                var directory = Path.GetDirectoryName(projectFilePath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(projectTemplateFullPath, projectFilePath);

                AssetDatabase.ImportAsset(projectAssetPath);
            }
            
            var projectAsset = AssetDatabase.LoadMainAssetAtPath(projectAssetPath) as AdvScenarioDataProject;

            return projectAsset;
        }

        private AdvImportScenarios GetImportScenarios(UtageContent content)
        {
            var scenarioFilePath = content.excelFilePath.Replace(ExcelExtension, ScenarioAssetExtension);
            var scenarioAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(scenarioFilePath);

            var importScenarioTemplateAssetPath = AssetDatabase.GetAssetPath(importScenarioTemplate);
            var importScenarioTemplateFullPath = UnityPathUtility.ConvertAssetPathToFullPath(importScenarioTemplateAssetPath);

            if (!File.Exists(scenarioFilePath))
            { 
                var directory = Path.GetDirectoryName(scenarioFilePath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(importScenarioTemplateFullPath, scenarioFilePath);

                AssetDatabase.ImportAsset(scenarioAssetPath);
            }
            
            var importBookAsset = AssetDatabase.LoadMainAssetAtPath(scenarioAssetPath) as AdvImportScenarios;

            importBookAsset.hideFlags = HideFlags.NotEditable;

            return importBookAsset;
        }

        private void SetChapterData(AdvScenarioDataProject project, string excelPath)
        {
            var excelAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(excelPath);

            var excelAsset = AssetDatabase.LoadMainAssetAtPath(excelAssetPath);

            project.ChapterDataList.Clear();

            var chapterData = new AdvScenarioDataProject.ChapterData()
            {
                chapterName = Path.GetFileNameWithoutExtension(excelPath),
                excelDir = null,
                excelList = new List<Object>(){ excelAsset },
            };

            project.ChapterDataList.Add(chapterData);

            UnityEditorUtility.SaveAsset(project);
        }

        /// <summary> Excelファイル群からUtageを出力する為の情報一覧生成 </summary>
        private UtageContent[] GetUtageContents()
        {
            var list = new List<UtageContent>();

            var excelFolderFullPath = UnityPathUtility.ConvertAssetPathToFullPath(excelFolderAssetPath) + PathUtility.PathSeparator;
            var exportFolderFullPath = UnityPathUtility.ConvertAssetPathToFullPath(exportFolderAssetPath) + PathUtility.PathSeparator;

            var directoryInfo = new DirectoryInfo(excelFolderFullPath);

            var files = directoryInfo.EnumerateFiles("*" + ExcelExtension, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var excelPath = PathUtility.ConvertPathSeparator(file.FullName);
                var exportPath = excelPath.Replace(excelFolderFullPath, exportFolderFullPath);

                var bookFilePath = Path.ChangeExtension(exportPath, AdvExcelImporter.BookAssetExt);

                var info = new UtageContent()
                {
                    excelFilePath = excelPath,
                    bookFilePath = bookFilePath,
                    excelLastWriteTime = file.LastWriteTime,
                };

                if (File.Exists(info.bookFilePath))
                {
                    info.bookFileLastWriteTime = File.GetLastWriteTime(info.bookFilePath);
                }

                list.Add(info);
            }

            return list.ToArray();
        }

        private bool IsRequireUpdate(UtageContent content, bool force)
        {
            // 強制アップデート.
            if (force){ return true; }

            // book.assetが存在しない.
            if (!content.bookFileLastWriteTime.HasValue){ return true; }
            
            // Excelファイルの方が更新日が新しい.
            return content.bookFileLastWriteTime < content.excelLastWriteTime;
        }

        private void MoveScenarioAsset(UtageContent content)
        {
            var fromDirectory = Path.GetDirectoryName(content.excelFilePath);
            var toDirectory = Path.GetDirectoryName(content.bookFilePath);

            void MoveAsset(string fileName)
            {
                var from = PathUtility.Combine(fromDirectory, fileName);
                var to = PathUtility.Combine(toDirectory, fileName);
                
                var fromAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(from);
                var toAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(to);

                if (!File.Exists(from)){ return; }

                if (!Directory.Exists(toDirectory))
                {
                    Directory.CreateDirectory(toDirectory);
                }

                if (UnityEditorUtility.IsExists(toAssetPath))
                {
                    AssetDatabase.DeleteAsset(toAssetPath);
                }

                AssetDatabase.MoveAsset(fromAssetPath, toAssetPath);

                AssetDatabase.Refresh();
            }

            using (new AssetEditingScope())
            {
                // .chapter.asset
                {
                    var chapterFilePath = content.bookFilePath.Replace(AdvExcelImporter.BookAssetExt, AdvExcelImporter.ChapterAssetExt);

                    var chapterFileName = Path.GetFileName(chapterFilePath);
                
                    MoveAsset(chapterFileName);
                }

                // .book.asset
                {
                    var chapterFileName = Path.GetFileName(content.bookFilePath);
                
                    MoveAsset(chapterFileName);
                }
            }
        }

        // Copy From: AdvScenarioDataBuilderWindow.cs
        private static void Import(AdvScenarioDataProject projectData)
        {
            if (projectData == null) { return; }

            var importer = new AdvExcelImporter();

            importer.ImportAll(projectData);

            if (projectData.IsAutoConvertOnImport)
            {
                Convert(projectData);
            }
        }
        
        // Copy From: AdvScenarioDataBuilderWindow.cs
        private static void Convert(AdvScenarioDataProject projectData)
        {
            if (projectData == null)
            {
                Debug.LogWarning("Scenario Data Excel project is no found");
                return;
            }

            if (string.IsNullOrEmpty(projectData.ConvertPath))
            {
                Debug.LogWarning("Convert Path is not defined");
                return;
            }
            
            var converter = new AdvExcelCsvConverter();

            foreach( var item in projectData.ChapterDataList )
            {
                if (!converter.Convert(projectData.ConvertPath, item.ExcelPathList, item.chapterName, projectData.ConvertVersion))
                {
                    Debug.LogWarning("Convert is failed");
                    return;
                }
            }

            if (projectData.IsAutoUpdateVersionAfterConvert)
            {
                projectData.ConvertVersion++;

                EditorUtility.SetDirty(projectData);
            }
        }
    }
}

#endif