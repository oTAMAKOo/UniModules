
using Cysharp.Threading.Tasks;
using Modules.Performance;

namespace Modules.ExternalAssets
{
	public interface IVersionFileHandler
	{
		UniTask<byte[]> Encode(byte[] bytes);

		UniTask<byte[]> Decode(byte[] bytes);
	}

	public sealed class DefaultVersionFileHandler : IVersionFileHandler
	{
		private FunctionFrameLimiter frameLimiter = null;

		public DefaultVersionFileHandler()
		{
			frameLimiter = new FunctionFrameLimiter(10000);
		}

		public async UniTask<byte[]> Encode(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				await frameLimiter.Wait();

				bytes[i] = (byte)~bytes[i];
			}

			return bytes;
		}

		public async UniTask<byte[]> Decode(byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; i++)
			{
				await frameLimiter.Wait();

				bytes[i] = (byte)~bytes[i];
			}

			return bytes;
		}
	}
}