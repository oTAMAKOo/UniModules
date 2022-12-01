
using System;
using System.Linq;

namespace Modules.AssetBundles
{
	public interface IAssetBundleFileHandler
	{
		byte[] Encode(byte[] bytes);

		byte[] Decode(byte[] bytes);
	}

    public sealed class DefaultAssetBundleFileHandler : IAssetBundleFileHandler
    {
		public byte[] Encode(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = (byte)~bytes[i];
			}
            
			return bytes;
		}

		public byte[] Decode(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = (byte)~bytes[i];
			}
            
			return bytes;
		}
	}
}