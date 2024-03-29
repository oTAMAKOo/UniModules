
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

        private string targetFolderAssetPath = null;

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

			targetFolderAssetPath = config.TargetFolderAssetPath;

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

					File.SetLastWriteTimeUtc(target.bookFilePath, DateTime.UtcNow);
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

            AdvScenarioDataBuilderWindow.ProjectData = project;
                
            Import(project);

			AdvScenarioDataBuilderWindow.ProjectData = null;

            AssetFileManager.IsEditorErrorCheck = originIsEditorErrorCheck;

			SaveScenarioAssets(content);
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

            if (projectAsset != null)
			{
				Reflection.SetPrivateField(projectAsset, "autoImportType", AdvScenarioDataProject.AutoImportType.None);

	            UnityEditorUtility.SaveAsset(projectAsset);
			}

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

            var targetFolderFullPath = UnityPathUtility.ConvertAssetPathToFullPath(targetFolderAssetPath) + PathUtility.PathSeparator;
            
            var directoryInfo = new DirectoryInfo(targetFolderFullPath);

            var files = directoryInfo.EnumerateFiles("*" + ExcelExtension, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var excelPath = PathUtility.ConvertPathSeparator(file.FullName);

                var bookFilePath = Path.ChangeExtension(excelPath, AdvExcelImporter.BookAssetExt);

                var info = new UtageContent()
                {
                    excelFilePath = excelPath,
                    bookFilePath = bookFilePath,
                    excelLastWriteTime = file.LastWriteTimeUtc,
                };

                if (File.Exists(info.bookFilePath))
                {
                    info.bookFileLastWriteTime = File.GetLastWriteTimeUtc(info.bookFilePath);
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
        
        private void Import(AdvScenarioDataProject projectData)
        {
            if (projectData == null) { return; }

			var importer = new AdvExcelImporter();

			importer.ImportAll(projectData);
        }

        private void SaveScenarioAssets(UtageContent content)
		{
            var directory = Path.GetDirectoryName(content.excelFilePath);

            var files = Directory.EnumerateFiles(directory, "*" + AdvExcelImporter.ScenarioAssetExt);

			foreach (var file in files)
			{
				var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(file);

                var guid = AssetDatabase.GUIDFromAssetPath(assetPath);

				AssetDatabase.SaveAssetIfDirty(guid);
			}

			AssetDatabase.Refresh();
		}
    }
}

#endif