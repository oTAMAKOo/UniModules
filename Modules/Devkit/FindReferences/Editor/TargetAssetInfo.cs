
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.FindReferences
{
	/// <summary> 依存される側の情報 </summary>
	public sealed class TargetAssetInfo
	{
		//----- params -----

		//----- field -----

		private string guid = null;
		private string fileId = null;

		//----- property -----

		public AssetReferenceInfo AssetReferenceInfo { get; private set; }

		//----- method -----

		internal TargetAssetInfo(Object target, string path, string fullPath)
		{
			this.guid = GetGuid(fullPath);
			this.AssetReferenceInfo = new AssetReferenceInfo(path, target);

			// DLLでMonoScriptだったらDLLの中のコンポーネントなのでfileIDを取り出す.
			if (path.EndsWith(".dll") && target is MonoScript)
			{
				fileId = UnityEditorUtility.GetLocalIdentifierInFile(target).ToString();
			}
		}

		/// <summary> 指定したアセットからこのアセットが参照されているかどうか返す. </summary>
		public bool IsReferencedFrom(AssetDependencyInfo dependencyInfo)
		{
			// fileIDがあるということはDLL.
			if (fileId != null)
			{
				// DLLの時はGUIDに加えてfileIDも比較.
				return dependencyInfo.FileIdsByGuid.ContainsKey(guid) && dependencyInfo.FileIdsByGuid[guid].Contains(fileId);
			}

			return dependencyInfo.FileIdsByGuid.ContainsKey(guid);
		}

		private static string GetGuid(string path)
		{
			using (var sr = new StreamReader(path + ".meta"))
			{
				while (!sr.EndOfStream)
				{
					var line = sr.ReadLine();
					var index = line.IndexOf("guid:", StringComparison.Ordinal);

					if (index >= 0)
					{
						return line.Substring(index + 6, 32);
					}
				}
			}

			return "0";
		}
	}
}