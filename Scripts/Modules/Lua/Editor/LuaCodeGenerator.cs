
using UnityEngine;
using System;
using XLua;
using CSObjectWrapEditor;
using Modules.Devkit.Console;

namespace Modules.Lua
{
    public static class LuaCodeGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public static void Generate()
		{
			try
			{
				DelegateBridge.Gen_Flag = true;

				#if !XLUA_GENERAL

				Generator.ClearAll();

				Generator.GenAll();

				#endif

				UnityConsole.Info("Generate lua bridge csharp code.");
			}
			catch (Exception e)
			{
				Debug.LogException(e);

				throw;
			}
		}
    }
}