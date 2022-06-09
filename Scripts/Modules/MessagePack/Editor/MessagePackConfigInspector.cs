﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
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
where mpc
where MsBuild";

				#endif

                EditorGUILayout.HelpBox(guideMessage, MessageType.Info);
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
                        UnityEditorUtility.RegisterUndo(instance);

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
                UnityEditorUtility.RegisterUndo(instance);

                serializedObject.ApplyModifiedProperties();
            }
            
            //------ CustomCodeGenerator ------

            EditorLayoutTools.ContentTitle("CodeGenerator");

            using (new ContentsScope())
            {
                EditorGUILayout.HelpBox("When the mpc command is not used.\n the code generator is directly specified for generation.", MessageType.Info);

                GUILayout.Label("Windows");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(winMpcRelativePath.stringValue, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "exe");

                        if (!string.IsNullOrEmpty(path))
                        {
                            winMpcRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(45f)))
                    {
                        winMpcRelativePath.stringValue = string.Empty;

                        serializedObject.ApplyModifiedProperties();
                    }
                }

                GUILayout.Label("MacOSX");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(osxMpcRelativePath.stringValue, pathTextStyle);

                    if (GUILayout.Button("Edit", GUILayout.Width(45f)))
                    {
                        UnityEditorUtility.RegisterUndo(instance);

                        var path = EditorUtility.OpenFilePanel("Select MessagePack compiler", Application.dataPath, "");

                        if (!string.IsNullOrEmpty(path))
                        {
                            osxMpcRelativePath.stringValue = UnityPathUtility.MakeRelativePath(path);

                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(45f)))
                    {
                        osxMpcRelativePath.stringValue = string.Empty;

                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            GUILayout.Space(4f);
        }

        private async UniTask FindDotnet()
        {
			if (isDotnetInstalled) { return; }

			findDotnet = false;
			isLoading = true;

            dotnetVersion = string.Empty;
			
			try
			{
				var dotNetPath = GetDotNetPath();

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

		private static string GetDotNetPath()
        {
	        var result = "dotnet";

			#if UNITY_EDITOR_WIN

	        // 環境変数.
	        var variable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);

	        if (variable != null)
	        {
		        foreach (var item in variable.Split(';'))
		        {
			        var path = PathUtility.Combine(item, "dotnet.exe");

			        if (!File.Exists(path)){ continue; }

			        result = path;

			        break;
		        }
	        }
			
			#endif

			#if UNITY_EDITOR_OSX

			var mpcPathCandidate = new string[]
			{
				"/usr/local/bin",
			};

			foreach (var item in mpcPathCandidate)
			{
				var path = PathUtility.Combine(item, "dotnet");

				if (!File.Exists(path)){ continue; }

				result = path;

				break;
			}

			#endif

	        return result;
        }
    }
}
