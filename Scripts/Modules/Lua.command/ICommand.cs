﻿
#if ENABLE_XLUA

namespace Modules.Lua.Command
{
	public interface ICommand
	{
		string LuaName { get; }

		string Callback { get; }
	}
}

#endif