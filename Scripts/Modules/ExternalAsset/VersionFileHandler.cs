
using Cysharp.Threading.Tasks;

namespace Modules.ExternalAssets
{
	public interface IVersionFileHandler
	{
		UniTask<byte[]> Encode(byte[] bytes);

		UniTask<byte[]> Decode(byte[] bytes);
	}

	public sealed class DefaultVersionFileHandler : IVersionFileHandler
	{
		private int count = 0;

		public async UniTask<byte[]> Encode(byte[] bytes)
		{
			return await Convert(bytes);
		}

		public async UniTask<byte[]> Decode(byte[] bytes)
		{
			return await Convert(bytes);
		}

		private async UniTask<byte[]> Convert(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				if (5000000 < count++)
				{
					count = 0;

					await UniTask.NextFrame();
				}

				bytes[i] = (byte)~bytes[i];
			}

			return bytes;
		}
	}
}