
#if ENABLE_XLUA

using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Lua.Text
{
    public sealed class LuaTextConfig : ReloadableScriptableObject<LuaTextConfig>
    {
        //----- params -----

		[Serializable]
		public sealed class TransferInfo
		{
			/// <summary> Assetsフォルダからの相対パス </summary>
			[SerializeField]
			public string sourceFolderRelativePath = null;
			[SerializeField]
			public string destFolderGuid = null;
		}

        //----- field -----

		[SerializeField]
		private FileLoader.Format fileFormat = FileLoader.Format.Yaml;

		[SerializeField]
		private string cryptoKey = null;
		[SerializeField]
		private string cryptoIv = null;

		#pragma warning disable 0414

		[SerializeField]
		private string winConverterPath = null;
		[SerializeField]
		private string osxConverterPath = null;

		#pragma warning restore 0414

        [SerializeField]
        private string workspaceRelativePath = null;
		[SerializeField]
		private string settingIniRelativePath = null;

		[SerializeField]
		private TransferInfo[] transferInfos = null;

		private AesCryptoKey aesCryptoKey = null;

        //----- property -----

		public FileLoader.Format Format { get { return fileFormat; } }

		public IReadOnlyList<TransferInfo> TransferInfos { get { return transferInfos; } }

        //----- method -----

		protected override void OnLoadInstance()
		{
			aesCryptoKey = null;
		}

		public AesCryptoKey GetCryptoKey()
		{
			return aesCryptoKey ?? (aesCryptoKey = new AesCryptoKey(cryptoKey, cryptoIv));
		}

		public string GetConverterPath()
		{
			var relativePath = string.Empty;

			#if UNITY_EDITOR_WIN
			
			relativePath = winConverterPath;

			#endif

			#if UNITY_EDITOR_OSX

			relativePath = osxConverterPath;
			
			#endif

			return string.IsNullOrEmpty(relativePath) ? null : UnityPathUtility.RelativePathToFullPath(relativePath);
		}

        public string GetWorkspacePath()
        {
            return UnityPathUtility.RelativePathToFullPath(workspaceRelativePath);
        }

		public string GetSettingsIniPath()
		{
			return UnityPathUtility.RelativePathToFullPath(settingIniRelativePath);
		}
	}
}

#endif