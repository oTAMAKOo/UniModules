
using Cysharp.Threading.Tasks;

namespace Modules.AssetBundles
{
	public interface IAssetBundleFileHandler
	{
		UniTask<byte[]> Encode(byte[] bytes);

		UniTask<byte[]> Decode(byte[] bytes);
	}

    public sealed class DefaultAssetBundleFileHandler : IAssetBundleFileHandler
    {
		public UniTask<byte[]> Encode(byte[] bytes)
		{
			return Convert(bytes);
		}

		public UniTask<byte[]> Decode(byte[] bytes)
		{
			return Convert(bytes);
		}

		private UniTask<byte[]> Convert(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = (byte)~bytes[i];
			}

			return UniTask.FromResult(bytes);
		}
	}
}