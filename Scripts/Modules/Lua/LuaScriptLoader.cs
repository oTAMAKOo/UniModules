
#if ENABLE_MOONSHARP

using System.Collections.Generic;
using Extensions;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace Modules.Lua
{
    public sealed class LuaScriptLoader : ScriptLoaderBase
    {
        //----- params -----

        //----- field -----
        
        private Dictionary<string, string> scripts = null;

        //----- property -----

        //----- method -----

        public LuaScriptLoader()
        {
            scripts = new Dictionary<string, string>();

            IgnoreLuaPathGlobal = false;

            ModulePaths = new string[] { "?" };
        }

        public void Register(string file, string script)
        {
            scripts.Add(file, script);
        }

        public void Remove(string file)
        {
            if (scripts.ContainsKey(file))
            {
                scripts.Remove(file);
            }
        }

        public override object LoadFile(string file, Table globalContext)
        {
            return scripts.GetValueOrDefault(file, string.Empty);
        }

        public override bool ScriptFileExists(string name)
        {
            return scripts.ContainsKey(name);
        }
    }
}

#endif