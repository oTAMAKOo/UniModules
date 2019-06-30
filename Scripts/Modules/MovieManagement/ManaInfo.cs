
#if ENABLE_CRIWARE_SOFDEC

using Extensions;

namespace Modules.MovieManagement
{
    public sealed class ManaInfo
    {
        public string Usm { get; private set; }
        public string UsmPath { get; private set; }

        public ManaInfo(string path)
        {
            this.Usm = path;
            this.UsmPath = PathUtility.GetPathWithoutExtension(path);
        }
    }
}

#endif
