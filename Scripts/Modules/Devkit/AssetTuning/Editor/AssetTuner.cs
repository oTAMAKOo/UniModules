
using UnityEngine;
using UnityEditor;
using System;
using Extensions;

namespace Modules.Devkit.AssetTuning
{
    public abstract class AssetTuner : LifetimeDisposable
    {
        public virtual int Priority { get { return 50; } }

        public abstract bool Validate(string assetPath);

        public virtual void OnBegin() { }

        public virtual void OnFinish() { }

        public virtual void OnAssetCreate(string assetPath) { }

        public virtual void OnAssetDelete(string assetPath) { }

        public virtual void OnAssetImport(string assetPath) { }

        public virtual void OnAssetMove(string assetPath, string from) { }
    }
}
