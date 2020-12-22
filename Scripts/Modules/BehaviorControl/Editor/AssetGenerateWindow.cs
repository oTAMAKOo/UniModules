
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;
using Modules.Devkit.Generators;
using Modules.Devkit.Prefs;

using Debug = UnityEngine.Debug;

namespace Modules.BehaviorControl
{
    public abstract class AssetGenerateWindow<TInstance, TBehaviorData, TAction, TTarget, TCondition> : SingletonEditorWindow<TInstance>
        where TInstance : AssetGenerateWindow<TInstance, TBehaviorData, TAction, TTarget, TCondition>
        where TBehaviorData : BehaviorData<TAction, TTarget, TCondition>, new()
        where TAction : Enum where TTarget : Enum where TCondition : Enum
    {
        //----- params -----

        private static class Prefs
        {
            public static string ImportDirectory
            {
                get { return ProjectPrefs.Get<string>("AssetGenerateWindowPrefs-importDirectory", null); }
                set { ProjectPrefs.Set("AssetGenerateWindowPrefs-importDirectory", value); }
            }
        }
        
        //----- field -----

        private string selectionDirectory = null;

        private string displayDirectory = null;

        private GUIStyle importPathStyle = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.Show(true);
        }

        private void Initialize()
        {
            if (initialized){ return; }

            titleContent = new GUIContent("BehaviorControl");

            minSize = new Vector2(280f, 80f);

            selectionDirectory = Prefs.ImportDirectory;

            displayDirectory = GetRelativeUriFromAssetsFolder(selectionDirectory);

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            var setting = BehaviorControlSetting.Instance;

            if (setting == null){ return; }

            var format = setting.Format;

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorLayoutTools.PrefixButton("ImportPath", GUILayout.Width(90f), GUILayout.Height(18f)))
                {
                    OpenSelectImportPathDialog(setting);
                }

                if (importPathStyle == null)
                {
                    importPathStyle = EditorStyles.textArea;
                    importPathStyle.alignment = TextAnchor.MiddleLeft;
                    importPathStyle.wordWrap = false;
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(3f);

                    EditorGUILayout.SelectableLabel(displayDirectory, importPathStyle, GUILayout.Height(18f));
                }
            }

            GUILayout.Space(3f);

            using (new DisableScope(string.IsNullOrEmpty(selectionDirectory)))
            {
                if (GUILayout.Button("Open Folder"))
                {
                    OpenFolder(selectionDirectory);
                }

                GUILayout.Space(2f);

                if (GUILayout.Button("Generate"))
                {
                    var importData = LoadImportData(setting, format);

                    if (importData != null)
                    {
                        Generate(setting, importData);

                        DeleteMissingSourceAsset(setting, importData.Keys.ToArray());
                    }
                }
            }
        }

        private void OpenSelectImportPathDialog(BehaviorControlSetting setting)
        {
            var importFolderPath = Prefs.ImportDirectory;

            if (string.IsNullOrEmpty(importFolderPath))
            {
                importFolderPath = setting.GetImportFolderPath();
            }

            selectionDirectory = null;

            var directory = EditorUtility.OpenFolderPanel("Import", importFolderPath, string.Empty);

            if (string.IsNullOrEmpty(directory)) { return; }

            if (!Directory.Exists(directory))
            {
                Debug.LogErrorFormat("Directory {0} not found.", importFolderPath);
            }

            selectionDirectory = directory;

            displayDirectory = GetRelativeUriFromAssetsFolder(directory);

            Prefs.ImportDirectory = directory;
        }

        private Dictionary<string, ImportData> LoadImportData(BehaviorControlSetting setting, FileLoader.Format fileFormat)
        {
            var extension = FileLoader.GetFileExtension(fileFormat);

            if (!Directory.Exists(selectionDirectory)){ return null; }

            var files = Directory.EnumerateFiles(selectionDirectory, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == extension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var dictionary = new Dictionary<string, ImportData>();

            foreach (var file in files)
            {
                var assetPath = ConvertDataPathToAssetPath(setting, file);

                var behaviorControlAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as BehaviorControlAsset;

                if (behaviorControlAsset != null && behaviorControlAsset.LastUpdate.HasValue)
                {
                    var fileLastUpdate = File.GetLastWriteTimeUtc(file);

                    var assetLastUpdate = behaviorControlAsset.LastUpdate.Value;

                    if (fileLastUpdate <= assetLastUpdate)
                    {
                        continue;
                    }
                }

                var data = FileLoader.LoadFile<ImportData>(file, fileFormat);

                if (data != null)
                {
                    dictionary.Add(file, data);
                }
            }

            return dictionary;
        }

        private void Generate(BehaviorControlSetting setting, Dictionary<string, ImportData> importData)
        {
            var success = true;

            var failedMessage = new StringBuilder();

            if (importData.Any())
            {
                failedMessage.AppendLine("Failed:");

                Action<string> onErrorCallback = message =>
                {
                    using (new DisableStackTraceScope(LogType.Error))
                    {
                        Debug.LogError(message);
                    }
                };
                
                var dataBuilder = new ImportDataConverter<TBehaviorData, TAction, TTarget, TCondition>(onErrorCallback);

                using (new AssetEditingScope())
                {
                    foreach (var item in importData)
                    {
                        var dataPath = item.Key;
                     
                        var assetPath = ConvertDataPathToAssetPath(setting, dataPath);

                        var behaviorData = dataBuilder.Convert(dataPath, item.Value);

                        if (behaviorData != null)
                        {
                            var lastUpdate = File.GetLastWriteTimeUtc(item.Key);

                            var result = CreateBehaviorDataAsset(assetPath, behaviorData, lastUpdate);

                            if (!result)
                            {
                                failedMessage.AppendFormat("- {0}", assetPath).AppendLine();
                                success = false;
                            }
                        }
                    }
                }
            }

            if (success)
            {
                UnityConsole.Info("Generate asset complete.");
            }
            else
            {
                Debug.LogError(failedMessage.ToString());
            }
        }

        private bool CreateBehaviorDataAsset(string assetPath, TBehaviorData behaviorData, DateTime lastUpdate)
        {
            var behaviorControlAsset = ScriptableObjectGenerator.Generate<BehaviorControlAsset>(assetPath, false);

            if (behaviorControlAsset == null) { return false; }

            var behaviors = new List<BehaviorControlAsset.Behavior>();

            foreach (var b in behaviorData.Behaviors)
            {
                var conditions = new List<BehaviorControlAsset.Condition>();

                foreach (var c in b.Conditions)
                {
                    var conditionType = GetEnumName(c.Type);
                    var conditionParameters = c.Parameters;
                    var connecter = c.Connecter;

                    var condition = new BehaviorControlAsset.Condition(conditionType, conditionParameters, connecter);

                    conditions.Add(condition);
                }

                var successRate = b.SuccessRate;
                var actionType = GetEnumName(b.ActionType);
                var actionParameters = b.ActionParameters;
                var targetType = GetEnumName(b.TargetType);
                var targetParameters = b.TargetParameters;

                var behavior = new BehaviorControlAsset.Behavior(successRate, actionType, actionParameters, targetType, targetParameters, conditions.ToArray());

                behaviors.Add(behavior);
            }

            behaviorControlAsset.Set(behaviorData.Description, behaviors.ToArray(), lastUpdate);

            UnityEditorUtility.SaveAsset(behaviorControlAsset);

            return true;
        }

        private void DeleteMissingSourceAsset(BehaviorControlSetting setting, string[] filePaths)
        {
            var exportFolderPath = setting.GetExportFolderPath();

            var deleteList = new List<string>();

            var existPaths = filePaths.Select(x => ConvertDataPathToAssetPath(setting, x)).ToArray();

            var assets = UnityEditorUtility.FindAssetsByType<BehaviorControlAsset>("t:BehaviorControlAsset", new[] { exportFolderPath }).ToArray();

            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);

                if (existPaths.Contains(assetPath)){ continue; }

                deleteList.Add(assetPath);
            }

            if (deleteList.Any())
            {
                var builder = new StringBuilder();

                builder.AppendLine("Delete missing source assets.");
                builder.AppendLine();

                using (new AssetEditingScope())
                {
                    foreach (var item in deleteList)
                    {
                        builder.AppendLine(item);

                        AssetDatabase.DeleteAsset(item);
                    }
                }

                Debug.LogWarning(builder.ToString());

                AssetDatabase.Refresh();
            }
        }

        private void OpenFolder(string folderPath)
        {
            folderPath = folderPath.Replace('/', Path.DirectorySeparatorChar);

            if (Directory.Exists(folderPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = folderPath,
                    FileName = "explorer.exe",
                };

                Process.Start(startInfo);
            }
            else
            {
                Debug.LogErrorFormat("{0} Directory does not exist!", folderPath);
            }
        }

        private string ConvertDataPathToAssetPath(BehaviorControlSetting setting, string dataPath)
        {
            var exportFolderPath = setting.GetExportFolderPath();

            var localPath = dataPath.Substring(selectionDirectory.Length);
            var exportPath = PathUtility.Combine(exportFolderPath, localPath);
            var assetPath = Path.ChangeExtension(exportPath, ".asset");

            return assetPath;
        }

        private string GetRelativeUriFromAssetsFolder(string directory)
        {
            var sPath1 = new Uri(Application.dataPath);
            var sPath2 = new Uri(directory);

            return sPath1.MakeRelativeUri(sPath2).ToString();
        }

        private static string GetEnumName<T>(T enumValue)
        {
            return Enum.GetName(typeof(T), enumValue);
        }
    }
}
