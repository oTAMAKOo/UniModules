
using System;

namespace Extensions
{
	public static class SafeValue
	{
		//----- params -----

		private static readonly byte seed = 0;

		//----- field -----

		//----- property -----

		//----- method -----

		static SafeValue()
		{
			var random = new Random();

			var bytes = new byte[1];

			random.NextBytes(bytes);

			seed = bytes[0];
		}

		public static void Pack(ref byte[] bytes, Func<byte[]> getBytes)
		{
			bytes = getBytes();

			Xor(ref bytes);
		}

		public static T UnPack<T>(byte[] bytes, byte[] buffer, Func<byte[], T> convert)
		{
			bytes.CopyTo(buffer, 0);

			Xor(ref buffer);

			var value = convert(buffer);

			return value;
		}

		private static void Xor(ref byte[] bytes)
		{
			for (var i = 0; i < bytes.Length; ++i)
			{
				bytes[i] ^= seed;
			}
		}
	}
}