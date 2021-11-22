﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
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

            if (isLoading) { return; }
            
            //------ .Netバージョン ------

            if (string.IsNullOrEmpty(dotnetVersion))
            {
                if (!isDotnetInstalled)
                {
                    EditorGUILayout.HelpBox(".NET Core SDK not found.", MessageType.Error);

                    #if UNITY_EDITOR_OSX
                    
                    EditorGUILayout.HelpBox("If installed and not found.\n\nTerminal run this command:\nln -s /usr/local/share/dotnet/dotnet /usr/local/bin/", MessageType.Info);

                    #endif

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

            #if UNITY_EDITOR_OSX
            
            EditorLayoutTools.ContentTitle("User local setting");

            using (new ContentsScope())
            {
                // Mpc.

                GUILayout.Label("Mpc Path");

                MessagePackConfig.Prefs.MpcPath = EditorGUILayout.DelayedTextField(MessagePackConfig.Prefs.MpcPath);

                // MSBuild.

                var message = string.Format("Environment variables need to be registered.\nPATH: {0}", DefaultMsBuildPath);

                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            #endif
        }

        private IEnumerator FindDotnet()
        {
            if (isDotnetInstalled) { yield break; }

            dotnetVersion = string.Empty;

            var processTitle = "MessagePackConfig";
            var processMessage = "Find .NET Core SDK";

            EditorUtility.DisplayProgressBar(processTitle, processMessage, 0f);

            Tuple<bool, string> result = null;

            var commandLineProcess = new ProcessExecute("dotnet", "--version");

            var findYield = commandLineProcess.StartAsync().ToObservable()
                .Do(x => result = Tuple.Create(true, x.Output))
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
