
using Cysharp.Threading.Tasks;

namespace Modules.Master
{
	public interface IVersionFileHandler
	{
		UniTask<byte[]> Encode(byte[] bytes);

		UniTask<byte[]> Decode(byte[] bytes);
	}

	public sealed class DefaultVersionFileHandler : IVersionFileHandler
	{
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
			try
			{
				await UniTask.RunOnThreadPool(() =>
				{
					for (var i = 0; i < bytes.Length; i++)
					{
						bytes[i] = (byte)~bytes[i];
					}
				});
			}
			finally
			{
				await UniTask.SwitchToMainThread();
			}

			return bytes;
		}
	}
}