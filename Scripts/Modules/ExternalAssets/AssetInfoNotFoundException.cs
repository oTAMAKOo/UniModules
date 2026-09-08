
using System;
using System.Runtime.Serialization;

namespace Modules.ExternalAssets
{
	[Serializable]
	public sealed class AssetInfoNotFoundException : Exception
	{
		public AssetInfoNotFoundException() : base() { }

		public AssetInfoNotFoundException(string message) : base(message) { }

		public AssetInfoNotFoundException(string message, Exception innerException) : base(message, innerException) { }

		private AssetInfoNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
