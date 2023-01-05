
using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Modules.ExternalAssets
{
	[Serializable]
    public sealed class AssetInfoNotFoundException : Exception
    {
		public string ResourcePath { get; private set; }
        
		public AssetInfoNotFoundException(string resourcePath) : base()
		{
			ResourcePath = resourcePath;

			Debug.LogError($"AssetInfo not found.\n{resourcePath}");
		}

		private AssetInfoNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}