
using Extensions;

namespace Modules.Devkit.AssetTuning
{
    public abstract class AssetTuner : LifetimeDisposable
    {
        public virtual int Priority { get { return 50; } }

        public abstract bool Validate(string assetPath);

        public virtual void OnAssetCreate(string assetPath) { }

        public virtual void OnAssetDelete(string assetPath) { }

		public virtual void OnPreprocessAsset(string assetPath) { }

		public virtual void OnBeforePostprocessAsset() { }

        public virtual void OnPostprocessAsset(string assetPath) { }

		public virtual void OnAfterPostprocessAsset() { }

        public virtual void OnAssetMove(string assetPath, string from) { }
    }
}
