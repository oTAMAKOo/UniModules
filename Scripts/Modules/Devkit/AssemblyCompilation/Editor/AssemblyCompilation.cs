
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.AssemblyCompilation
{
    public abstract class AssemblyCompilation<TInstance> : Singleton<TInstance> where TInstance : AssemblyCompilation<TInstance>
    {
        //----- params -----

        private sealed class Prefs
        {
            public static bool RequestCompile
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-RequestCompile", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-RequestCompile", value); }
            }

            public static string Result
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-Result", string.Empty); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-Result", value); }
            }
        }

        public sealed class CompileResult
        {
            /// <summary> アセンブリ名. </summary>
            public string Assembly { get; private set; }
			/// <summary> エラー. </summary>
            public CompilerMessage[] Messages { get; private set; }

            public CompileResult(string assembly, CompilerMessage[] messages)
            {
                Assembly = assembly;
                Messages = messages;
            }
        }

        //----- field -----

        private Dictionary<string, CompileResult> compileResults = null;

        //----- property -----

        //----- method -----

		public void RequestCompile()
		{
			// コンパイル中は予約だけして実行しない.
			if (EditorApplication.isCompiling)
			{
				Prefs.RequestCompile = true;

				return;
			}

			Prefs.RequestCompile = false;

			// コンパイル要求.
			UnityEditorUtility.RequestScriptCompilation();

			CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
			CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

			AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
			AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

			compileResults = new Dictionary<string, CompileResult>();
		}

		public void SetBuildTarget(BuildTarget buildTarget)
		{
			if(Application.isBatchMode)
			{
				var errorMessage = @"This method is not available when running Editor in batch mode.\nUse the buildTarget command line switch to set the build target to use in batch mode.";

				Debug.LogError(errorMessage);

				return;
			}

			var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

			if (EditorUserBuildSettings.selectedBuildTargetGroup != buildTargetGroup ||
				EditorUserBuildSettings.activeBuildTarget != buildTarget)
			{
				EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
			}
		}

		public void SetScriptingDefineSymbols(BuildTarget buildTarget, string defineSymbols)
		{
			var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

			var currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

			if (currentDefineSymbols != defineSymbols)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);
			}
		}

		protected void OnAssemblyReload()
        {
			// 起動時にリセット.
			if (EditorApplication.timeSinceStartup < 3)
			{
				Prefs.RequestCompile = false;
				Prefs.Result = null;
			}

            if (Prefs.RequestCompile)
            {
                RequestCompile();
            }
            else
            {
                var json = Prefs.Result;

                if (!string.IsNullOrEmpty(json))
                {
                    var results = JsonConvert.DeserializeObject<CompileResult[]>(json);

                    EditorApplication.LockReloadAssemblies();
                    
                    OnCompileFinished(results);

                    EditorApplication.UnlockReloadAssemblies();

                    Prefs.Result = null;
                }
            }
        }

		private void OnAssemblyCompilationFinished(string assemblyName, CompilerMessage[] messages)
        {
            var result = new CompileResult(assemblyName, messages);

            compileResults[assemblyName] = result;
        }

        private void BeforeAssemblyReload()
        {
            var results = compileResults.Values.ToArray();

            Prefs.Result = results.ToJson();
        }

        protected abstract void OnCompileFinished(CompileResult[] results);
    }
}
