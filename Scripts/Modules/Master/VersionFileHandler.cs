
namespace Modules.Master
{
    public interface IVersionFileHandler
    {
        byte[] Encode(byte[] bytes);

        byte[] Decode(byte[] bytes);
    }

    public sealed class DefaultVersionFileHandler : IVersionFileHandler
    {
        public byte[] Encode(byte[] bytes)
        {
            return Convert(bytes);
        }

        public byte[] Decode(byte[] bytes)
        {
            return Convert(bytes);
        }

        private byte[] Convert(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)~bytes[i];
            }

            return bytes;
        }
    }
}