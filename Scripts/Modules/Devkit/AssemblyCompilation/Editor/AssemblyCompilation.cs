
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
            public static bool reserve
            {
                get { return ProjectPrefs.GetBool("AssemblyCompilation-reserve", false); }
                set { ProjectPrefs.SetBool("AssemblyCompilation-reserve", value); }
            }

            public static string result
            {
                get { return ProjectPrefs.GetString("AssemblyCompilation-result", string.Empty); }
                set { ProjectPrefs.SetString("AssemblyCompilation-result", value); }
            }
        }

        public sealed class CompileResult
        {
            /// <summary> アセンブリ名. </summary>
            public string Assembly { get; private set; }
            /// <summary> 時間. </summary>
            public double Time { get; private set; }
            /// <summary> エラー. </summary>
            public CompilerMessage[] Messages { get; private set; }

            public CompileResult(string assembly, double time, CompilerMessage[] messages)
            {
                Assembly = assembly;
                Time = time;
                Messages = messages;
            }
        }

        //----- field -----

        private static Dictionary<string, double> timeStamps = null;

        private static Dictionary<string, CompileResult> compileResults = null;

        //----- property -----

        //----- method -----

        protected void OnAssemblyReload()
        {
            if (Prefs.reserve)
            {
                RequestCompilation();
            }
            else
            {
                var json = Prefs.result;

                if (!string.IsNullOrEmpty(json))
                {
                    var results = JsonConvert.DeserializeObject<CompileResult[]>(json);

                    EditorApplication.LockReloadAssemblies();
                    
                    OnCompilationFinished(results);

                    EditorApplication.UnlockReloadAssemblies();

                    Prefs.result = null;
                }
            }
        }

        protected void RequestCompilation()
        {
            // コンパイル中は予約だけして実行しない.
            if (EditorApplication.isCompiling)
            {
                Prefs.reserve = true;

                return;
            }

            Prefs.reserve = false;

            // コンパイル要求.
            UnityEditorUtility.RequestScriptCompilation();

            CompilationPipeline.assemblyCompilationStarted -= OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            timeStamps = new Dictionary<string, double>();

            compileResults = new Dictionary<string, CompileResult>();
        }

        private void OnAssemblyCompilationStarted(string assemblyName)
        {
            timeStamps[assemblyName] = EditorApplication.timeSinceStartup;
        }

        private void OnAssemblyCompilationFinished(string assemblyName, CompilerMessage[] messages)
        {
            var time = EditorApplication.timeSinceStartup - timeStamps[assemblyName];

            var result = new CompileResult(assemblyName, time, messages);

            compileResults[assemblyName] = result;
        }

        private void BeforeAssemblyReload()
        {
            var results = compileResults.Values.ToArray();

            Prefs.result = results.ToJson();
        }

        protected abstract void OnCompilationFinished(CompileResult[] results);
    }
}
