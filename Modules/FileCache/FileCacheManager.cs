
namespace Modules.FileCache
{
    public sealed class FileCacheManager : FileCacheManagerBase<FileCacheManager>
    {
        public void Save(byte[] bytes, string source, ulong updateAt, ulong expireAt)
        {
            CreateCache(bytes, source, updateAt, expireAt);
        }

        public byte[] Load(string source)
        {
            return LoadCache(source);
        }
    }
}