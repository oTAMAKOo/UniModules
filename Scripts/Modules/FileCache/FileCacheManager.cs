
namespace Modules.FileCache
{
    public sealed class FileCacheManager : FileCacheManagerBase<FileCacheManager>
    {
        public void Save(byte[] bytes, string source, long updateAt, long expireAt)
        {
            CreateCache(bytes, source, updateAt, expireAt);
        }

        public byte[] Load(string source)
        {
            return LoadCache(source);
        }
    }
}