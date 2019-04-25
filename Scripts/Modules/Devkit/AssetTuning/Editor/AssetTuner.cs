
using UnityEngine;
using UnityEditor;
using System;
using Extensions;

namespace Modules.Devkit.AssetTuning
{
    public abstract class AssetTuner : LifetimeDisposable
    {
        public virtual int Priority { get { return 50; } }

        public abstract bool Validate(string path);

        public virtual void OnBegin() { }

        public virtual void OnFinish() { }

        public virtual void OnAssetCreate(string path) { }

        public virtual void OnAssetDelete(string path) { }

        public virtual void OnAssetImport(string path) { }

        public virtual void OnAssetMove(string path, string from) { }
    }
}
