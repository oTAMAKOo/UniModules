
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Lua
{
    public sealed class LuaReference : ScriptableObject
    {
        //----- params -----

		[Serializable]
		public sealed class Info
		{
			public string path = null;
			public LuaAsset asset = null;
			public bool autoload = false;
		}

		//----- field -----

        [SerializeField]
        private Info[] infos = null;

        //----- property -----

		public Info[] Infos { get { return infos; } }

        //----- method -----

        public LuaAsset GetLuaAsset(string path)
        {
			if (infos == null){ return null; }

			var info = infos.FirstOrDefault(x => x.path == path);

			if (info == null){ return null; }

            return info.asset;
        }
    }
}
