
using UnityEngine;
using Extensions;
using XLua;

namespace Modules.Lua
{
	public static class LuaExtensions
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		#region LuaEnv

		public static object[] Require(this LuaEnv luaEnv, string luaPath)
		{
			return luaEnv.DoString($"require('{ luaPath }')");
		}

		public static object[] Request(this LuaEnv luaEnv, string luaPath)
		{
			return luaEnv.DoString($"request('{ luaPath }')");
		}

		#endregion

		#region LuaAsset

		/// <summary> <see cref="LuaAsset.GetDecodeBytes"/>の実装が中途半端な状態で正しくデータが取れないのを回避 </summary>
		public static byte[] GetData(this LuaAsset luaAsset)
		{
			return luaAsset.encode ? Security.XXTEA.Decrypt(luaAsset.data, LuaAsset.LuaDecodeKey) : luaAsset.data;
		}

		#endregion
	}
}