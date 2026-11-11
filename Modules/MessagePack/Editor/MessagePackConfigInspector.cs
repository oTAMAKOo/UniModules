
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;

namespace Modules.MessagePack
{
    [CustomEditor(typeof(MessagePackConfig), true)]
    public sealed class MessagePackConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private GUIStyle pathTextStyle = null;

        private MessagePackConfig instance = null;

        private bool isLoading = false;
        private bool findDotnet = false;

        [NonSerialized]
        private bool initialized = false;

        private static bool isDotnetInstalled = false;
        private static string dotnetVersion = null;

        //----- property -----

        //----- method -----

        private void Initialize()
        {
            if (initialized) { return; }

            if (pathTextStyle == null)
            {
                pathTextStyle = GUI.skin.GetStyle("TextArea");
                pathTextStyle.alignment = TextAnchor.MiddleLeft;
            }

            initialized = true;
        }

        void OnEnable()
        {
            findDotnet = true;
        }
        
        public override void OnInspectorGUI()
        {
            instance = target as MessagePackConfig;

            Initialize();
            
            if (findDotnet)
            {
                FindDotnet().Forget();
            }

            serializedObject.Update();

            var winMpcSetting = Reflection.GetPrivateField<MessagePackConfig, MessagePackConfig.MpcSetting>(instance, "winMpcSetting");
            var osxMpcSetting = Reflection.GetPrivateField<MessagePackConfig, MessagePackConfig.MpcSetting>(instance, "osxMpcSetting");

            var codeGenerateTarget = serializedObject.FindProperty("codeGenerateTarget");
            var scriptExportAssetDir = serializedObject.FindProperty("scriptExportAssetDir");
            var scriptName = serializedObject.FindProperty("scriptName");
            var useMapMode = serializedObject.FindProperty("useMapMode");
            var resolverNameSpace = serializedObject.FindProperty("resolverNameSpace");
            var resolverName = serializedObject.FindProperty("resolverName");
            var conditionalCompilerSymbols = serializedObject.FindProperty("conditionalCompilerSymbols");
            var forceAddGlobalSymbols = serializedObject.FindProperty("forceAddGlobalSymbols");

            if (isLoading) { return; }
            
            //------ .Netバージョン ------

            if (string.IsNullOrEmpty(dotnetVersion))
            {
                if (!isDotnetInstalled)
                {
                    EditorGUILayout.HelpBox(".NET Core SDK not found.", MessageType.Error);

                    // インストールページ.
                    if (GUILayout.Button("Open .NET Core install page."))
                    {
                        Application.OpenURL("https://dotnet.microsoft.com/download");
                    }

                    EditorGUILayout.Separator();
                }
            }
            else
            {
                var guideMessage = $".NET Core SDK {dotnetVersion}";

                #if UNITY_EDITOR_OSX

                guideMessage += @"
Require add path from terminal command :
where dotnet
where MsBuild

Require fix dotnet version : 
https://docs.microsoft.com/ja-jp/dotnet/core/tools/global-json
csproj directory : global.json
";

                #endif

                EditorGUILayout.HelpBox(guideMessage, MessageType.Info);
            }

            //------ CustomCodeGenerator ------

            EditorLayoutTools.ContentTitle("CodeGenerator Settings");

            using (new ContentsScope())
            {
                var privateSetting = MessagePackConfig.Prefs.PrivateSetting;

                using (new DisableScope(privateSetting != null))
                {
                    {
                        var changed = DrawMpcSettingGUI("win", winMpcSetting);

                        if (changed)
                        {
                            Reflection.SetPrivateField(instance, "winMpcSetting", winMpcSetting);
                            UnityEditorUtility.SaveAsset(instance);
                        }
                    }

                    {
                        var changed = DrawMpcSettingGUI("osx", osxMpcSetting);

                        if (changed)
                        {
                            Reflection.SetPrivateField(instance, "osxMpcSetting", osxMpcSetting);
                            UnityEditorUtility.SaveAsset(instance);
                        }
                    }
                }

                {
                    EditorGUI.BeginChangeCheck();

                    var toggle = EditorGUILayout.Toggle("Use private setting", privateSetting != null);

                    if (EditorGUI.EndChangeCheck())
                    {
                        MessagePackConfig.Prefs.PrivateSetting = toggle ? new MessagePackConfig.MpcSetting() : null;
                    }

                    if (privateSetting != null)
                    {
                        var changed = DrawMpcSettingGUI("private", privateSetting);

                        if (changed)
                        {
                            MessagePackConfig.Prefs.PrivateSetting = privateSetting;
                        }
                    }
                }
            }

            GUILayout.Space(4f);

            //------ 基本設定 ------

            EditorLayoutTools.ContentTitle("CodeGenerator Settings");

            using (new ContentsScope())
            {
                GUILayout.Label("Code Generate Target");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    codeGenerateTarget.stringValue = EditorGUILayout.DelayedTextField(codeGenerateTarget.stringValue, pathTextStyle);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        serializedObject.ApplyModifiedProperties();
                    }

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        EditorApplication.delayCall += () =>
                        {
                            var path = EditorUtility.OpenFilePanel("Select code generate target", UnityPathUtility.DataPath, string.Empty);

                            codeGenerateTarget.stringValue = UnityPathUtility.MakeRelativePath(path);

                            serializedObject.ApplyModifiedProperties();
                        };
                    }
                }

                GUILayout.Label("Export Folder");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    scriptExportAssetDir.stringValue = EditorGUILayout.DelayedTextField(scriptExportAssetDir.stringValue, pathTextStyle);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        serializedObject.ApplyModifiedProperties();
                    }

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        EditorApplication.delayCall += () =>
                        {
                            var path = EditorUtility.OpenFolderPanel("Select export folder", Application.dataPath, string.Empty);

                            scriptExportAssetDir.stringValue = UnityPathUtility.MakeRelativePath(path);

                            serializedObject.ApplyModifiedProperties();
                        };
                    }
                }

                EditorGUI.BeginChangeCheck();

                GUILayout.Label("Export FileName");

                scriptName.stringValue = EditorGUILayout.DelayedTextField(scriptName.stringValue);

                GUILayout.Space(2f);

                useMapMode.boolValue = EditorGUILayout.Toggle("MapMode", useMapMode.boolValue);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUILayout.Space(4f);

            //------ オプション設定 ------

            EditorLayoutTools.ContentTitle("CodeGenerator Options");

            EditorGUI.BeginChangeCheck();

            using (new ContentsScope())
            {
                GUILayout.Label("NameSpace");

                resolverNameSpace.stringValue = EditorGUILayout.DelayedTextField(resolverNameSpace.stringValue);

                GUILayout.Label("ClassName");

                resolverName.stringValue = EditorGUILayout.DelayedTextField(resolverName.stringValue);

                GUILayout.Label("Conditional Compiler Symbols");

                conditionalCompilerSymbols.stringValue = EditorGUILayout.DelayedTextField(conditionalCompilerSymbols.stringValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.Space(4f);

            //------ ForceAddGlobalSymbols ------

            EditorLayoutTools.ContentTitle("Mpc generated cs file edit");

            using (new ContentsScope())
            {
                EditorGUILayout.HelpBox("Fix : Missing global::xxxxx namespace error.", MessageType.Info);

                EditorGUI.BeginChangeCheck();

                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(forceAddGlobalSymbols);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUILayout.Space(4f);
        }

        private bool DrawMpcSettingGUI(string title, MessagePackConfig.MpcSetting setting)
        {
            var changed = false;

            EditorLayoutTools.ContentTitle(title);

            EditorGUI.BeginChangeCheck();

            using (new ContentsScope())
            {
                GUILayout.Label("ProcessCommand");

                setting.processCommand = EditorGUILayout.DelayedTextField(setting.processCommand);

                GUILayout.Space(2f);

                GUILayout.Label("MessagePack Compiler");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(setting.mpcRelativePath, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        EditorApplication.delayCall += () =>
                        {
                            var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", UnityPathUtility.DataPath, string.Empty);

                            if (!string.IsNullOrEmpty(path))
                            {
                                setting.mpcRelativePath = UnityPathUtility.MakeRelativePath(path);
                                
                                changed = true;
                            }
                        };
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(45f)))
                    {
                        setting.mpcRelativePath = string.Empty;

                        changed = true;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }

            return changed;
        }

        private async UniTask FindDotnet()
        {
            if (isDotnetInstalled) { return; }

            findDotnet = false;
            isLoading = true;

            dotnetVersion = string.Empty;
            
            try
            {
                var dotNetPath = MessagePackHelper.GetDotNetPath();

                var commandLineProcess = new ProcessExecute(dotNetPath, "--version");

                var result = await commandLineProcess.StartAsync().AsUniTask();

                dotnetVersion = result.Output;

                if (!string.IsNullOrEmpty(dotnetVersion))
                {
                    var major = int.Parse(dotnetVersion.Split('.').First());

                    isDotnetInstalled = 3 <= major;
                }
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                isLoading = false;
            }

            if (string.IsNullOrEmpty(dotnetVersion))
            {
                Debug.LogError("Failed get .NET Core SDK version.");
            }
            
            Repaint();

            EditorApplication.delayCall += EditorUtility.ClearProgressBar;
        }
    }
}
