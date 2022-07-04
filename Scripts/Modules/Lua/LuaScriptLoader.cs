
#if ENABLE_MOONSHARP

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions;
using UniRx;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using UnityEngine;

namespace Modules.Lua.legacy
{
    public abstract class LuaScriptLoader : ScriptLoaderBase
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public Dictionary<string, string> Scripts { get; private set; }

        //----- method -----

        public LuaScriptLoader()
        {
            Scripts = new Dictionary<string, string>();

            IgnoreLuaPathGlobal = false;

            ModulePaths = new string[] { "?" };
        }

        public void SetSubScript(string fileName, string script)
        {
            if (string.IsNullOrEmpty(fileName)) { return; }

            if (string.IsNullOrEmpty(script)) { return; }

            Scripts[fileName] = script;
        }

        public void ReleaseScript(string fileName)
        {
            if (Scripts.ContainsKey(fileName))
            {
                Scripts.Remove(fileName);
            }
        }

        public void ReleaseAllScript()
        {
            Scripts.Clear();
        }

        public IObservable<Unit> LoadSubScripts(string script)
        {
            var fileNames = GetRequireFileNames(script);

            Action<string, string> onLoadComplete = (f, s) =>
            {
                if (string.IsNullOrEmpty(s)) { return; }

                Scripts[f] = s;
            };

            return fileNames.Select(x => LoadScript(x).Do(y => onLoadComplete(x, y)))
                .WhenAll()
                .AsUnitObservable();
        }
        
        public override object LoadFile(string file, Table globalContext)
        {
            var script = Scripts.GetValueOrDefault(file, string.Empty);

            if (string.IsNullOrEmpty(script))
            {
                Debug.LogErrorFormat("Empty lua script.\n{0}", file);
            }

            return script;
        }

        public override bool ScriptFileExists(string name)
        {
            var exists = Scripts.ContainsKey(name);

            if (!exists)
            {
                Debug.LogErrorFormat("Undefined lua file.\n{0}", name);
            }

            return exists;
        }

        private string[] GetRequireFileNames(string script)
        {
            var startIndex = 0;
            var list = new List<string>();
            var fileNameBuilder = new StringBuilder();

            using (var rs = new StringReader(script))
            {
                var funcName = "require";

                while (-1 < rs.Peek())
                {
                    startIndex = 0;

                    var line = rs.ReadLine();

                    while (startIndex < line.Length)
                    {
                        var index = line.IndexOf(funcName, startIndex, StringComparison.Ordinal);

                        if (index == -1) { break; }
                        
                        startIndex = index + funcName.Length;
                        
                        fileNameBuilder.Clear();

                        var step = 0;

                        for (var i = index; i < line.Length; i++)
                        {
                            var c = line[i];

                            switch (step)
                            {
                                case 0:
                                    if (c == '(') { step = 1; }
                                    break;

                                case 1:
                                    if (c == '\'' || c == '\"') { step = 2; }
                                    break;

                                case 2:
                                    if (c != '\'' && c != '\"')
                                    {
                                        fileNameBuilder.Append(c);
                                    }
                                    else
                                    {
                                        step = 3;
                                    }
                                    break;

                                case 3:
                                    {
                                        var fileName = fileNameBuilder.ToString();

                                        fileName = fileName.Trim();

                                        if (!string.IsNullOrEmpty(fileName))
                                        {
                                            list.Add(fileName);
                                        }

                                        step = 0;
                                        fileNameBuilder.Clear();
                                    }
                                    break;
                            }
                        }

                        if (step != 0)
                        {
                            throw new FormatException();
                        }
                    }
                }
            }

            return list.ToArray();
        }

        protected abstract IObservable<string> LoadScript(string script);
    }
}

#endif