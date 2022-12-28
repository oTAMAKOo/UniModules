
#if ENABLE_CRIWARE_SOFDEC

using Extensions;

namespace Modules.Movie
{
    public sealed class ManaInfo
    {
		public string Usm { get; private set; }
        public string UsmPath { get; private set; }

        public ManaInfo(string filePath)
        {
			this.Usm = filePath;
            this.UsmPath = PathUtility.GetPathWithoutExtension(filePath);
        }
    }
}

#endif
