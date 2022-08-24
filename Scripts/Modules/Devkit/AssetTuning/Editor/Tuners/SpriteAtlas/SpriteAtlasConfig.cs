
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class SpriteAtlasConfig :  ReloadableScriptableObject<SpriteAtlasConfig>
    {
        //----- params -----

        //----- field -----

		[SerializeField]
		private Object[] disableIncludeInBuildFolders = null;

        //----- property -----

		/// <summary> IncludeInBuildを無効にするフォルダ. </summary>
		public Object[] DisableIncludeInBuildFolders
		{
			get { return disableIncludeInBuildFolders ?? (disableIncludeInBuildFolders = new Object[0]); }
		}

        //----- method -----
    }
}