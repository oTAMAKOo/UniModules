﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.MessagePack
{
    [CustomEditor(typeof(MessagePackConfig), true)]
    public sealed class MessagePackConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        private const string RequireDotnetSDKVersion = "3.1";
        
        private const string DefaultMsBuildPath = "/Library/Frameworks/Mono.framework/Versions/Current/bin";

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
                isLoading = true;
                findDotnet = false;

                Observable.FromMicroCoroutine(() => FindDotnet()).Subscribe(_ => isLoading = false);
            }

            serializedObject.Update();

            var scriptExportAssetDir = serializedObject.FindProperty("scriptExportAssetDir");
            var scriptName = serializedObject.FindProperty("scriptName");
            var useMapMode = serializedObject.FindProperty("useMapMode");
            var resolverNameSpace = serializedObject.FindProperty("resolverNameSpace");
            var resolverName = serializedObject.FindProperty("resolverName");
            var conditionalCompilerSymbols = serializedObject.FindProperty("conditionalCompilerSymbols");

            var winMpcRelativePath = serializedObject.FindProperty("winMpcRelativePath");
            var osxMpcRelativePath = serializedObject.FindProperty("osxMpcRelativePath");

            if (isLoading) { return; }

            //------ .Netバージョン ------

            if (string.IsNullOrEmpty(dotnetVersion))
            {
                if (!isDotnetInstalled)
                {
                    EditorGUILayout.HelpBox(".NET Core SDK not found.\nMessagePack CodeGen requires .NET Core Runtime.", MessageType.Error);

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
                EditorGUILayout.HelpBox(string.Format(".NET Core SDK {0}(Require Version {1})", dotnetVersion, RequireDotnetSDKVersion), MessageType.Info);
            }

            //------ 基本設定 ------

            EditorLayoutTools.ContentTitle("MessagePack Script Export");

            using (new ContentsScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(scriptExportAssetDir.stringValue, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                        var path = EditorUtility.OpenFolderPanel("Select Export Folder", Application.dataPath, string.Empty);

                        scriptExportAssetDir.stringValue = UnityPathUtility.MakeRelativePath(path);

                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUI.BeginChangeCheck();

                GUILayout.Label("Export FileName");

                scriptName.stringValue = EditorGUILayout.DelayedTextField(scriptName.stringValue);

                useMapMode.boolValue = EditorGUILayout.Toggle("MapMode", useMapMode.boolValue);
            }

            GUILayout.Space(4f);

            //------ コードジェネレーター ------

            EditorLayoutTools.ContentTitle("CodeGenerator");

            using (new ContentsScope())
            {
                GUILayout.Label("Windows");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(winMpcRelativePath.stringValue, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                        var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "exe");

                        winMpcRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                        serializedObject.ApplyModifiedProperties();
                    }
                }

                GUILayout.Label("MacOSX");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(osxMpcRelativePath.stringValue, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                        var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "");

                        osxMpcRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            GUILayout.Space(4f);

            //------ オプション設定 ------

            EditorLayoutTools.ContentTitle("CodeGenerator Options");

            using (new ContentsScope())
            {
                GUILayout.Label("NameSpace");

                resolverNameSpace.stringValue = EditorGUILayout.DelayedTextField(resolverNameSpace.stringValue);

                GUILayout.Label("ClassName");

                resolverName.stringValue = EditorGUILayout.DelayedTextField(resolverName.stringValue);

                GUILayout.Label("Conditional Compiler Symbols");

                conditionalCompilerSymbols.stringValue = EditorGUILayout.DelayedTextField(conditionalCompilerSymbols.stringValue);
            }

            GUILayout.Space(4f);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);

                serializedObject.ApplyModifiedProperties();
            }

            //------ User local setting ------

            var platform = Environment.OSVersion.Platform;

            if (platform == PlatformID.MacOSX || platform == PlatformID.Unix)
            {
                EditorLayoutTools.ContentTitle("User local setting");

                using (new ContentsScope())
                {
                    EditorGUI.BeginChangeCheck();
    
                    // DotNet.

                    GUILayout.Label("DotNet Path");

                    MessagePackConfig.Prefs.DotnetPath = EditorGUILayout.DelayedTextField(MessagePackConfig.Prefs.DotnetPath);

                    // MSBuild.
                    
                    GUILayout.Label("MSBuild");

                    var message = string.Format("Environment variables need to be registered.\nPATH: {0}", DefaultMsBuildPath);

                    EditorGUILayout.HelpBox(message, MessageType.Info);
                 
                    if(EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("MessagePackConfigInspector Undo", instance);
                    }
                }
            }
        }

        private IEnumerator FindDotnet()
        {
            if (isDotnetInstalled) { yield break; }

            dotnetVersion = string.Empty;

            var processTitle = "MessagePackConfig";
            var processMessage = "Find .NET Core SDK";

            EditorUtility.DisplayProgressBar(processTitle, processMessage, 0f);

            Tuple<bool, string> result = null;

            var command = string.Empty;
            var arguments = string.Empty;

            var platform = Environment.OSVersion.Platform;

            switch (platform)
            {
                case PlatformID.Win32NT:
                    {
                        command = "dotnet";
                        arguments = "--version";
                    }
                    break;

                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    {
                        var dotnetPath = MessagePackConfig.Prefs.DotnetPath;

                        command = "/bin/bash";
                        arguments = $"-c \"{dotnetPath} --version\"";
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            var commandLineProcess = new ProcessExecute(command, arguments);

            var findYield = commandLineProcess.StartAsync().ToObservable()
                .Do(x => result = Tuple.Create(true, x.Item2))
                .DoOnError(x => result = Tuple.Create(false, (string)null))
                .Select(_ => result)
                .ToYieldInstruction();

            while (!findYield.IsDone)
            {
                yield return null;
            }

            if (findYield.HasResult)
            {
                dotnetVersion = findYield.Result.Item2;

                isDotnetInstalled = findYield.Result.Item1 && !string.IsNullOrEmpty(dotnetVersion);
            }

            if (string.IsNullOrEmpty(dotnetVersion))
            {
                Debug.LogError("Failed get .NET Core SDK version.");
            }

            EditorUtility.DisplayProgressBar(processTitle, processMessage, 1f);

            Repaint();

            EditorApplication.delayCall += EditorUtility.ClearProgressBar;
        }
    }
}
