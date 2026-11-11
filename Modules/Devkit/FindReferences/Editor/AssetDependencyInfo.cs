
using System.Collections.Generic;

namespace Modules.Devkit.FindReferences
{
	/// <summary> 参照を持っているアセット(依存を持つ側)の情報. </summary>
	public sealed class AssetDependencyInfo
	{
		public string FullPath { get; private set; }

		/// <summary> 参照しているコンポーネントのGUIDとfileIDのセット. </summary>
		public Dictionary<string, HashSet<string>> FileIdsByGuid { get; private set; }

		public AssetDependencyInfo(string fullPath, Dictionary<string, HashSet<string>> fileIdsByGuid)
		{
			FullPath = fullPath;
			FileIdsByGuid = fileIdsByGuid;
		}
	}
}