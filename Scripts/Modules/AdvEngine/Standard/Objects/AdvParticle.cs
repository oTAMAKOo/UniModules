
using UnityEngine;
using System;
using UniRx;
using Extensions;
using Modules.Particle;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvParticle : AdvObject
    {
        #if ENABLE_MOONSHARP

        //----- params -----

        //----- field -----

        private ParticlePlayer particlePlayer = null;

        //----- property -----

        //----- method -----

        public IObservable<Unit> Play(string fileName, bool restart, int? sortingOrder)
        {
            var advEngine = AdvEngine.Instance;

            if (particlePlayer != null)
            {
                UnityUtility.SafeDelete(particlePlayer.gameObject);
            }

            var resourcePath = advEngine.Resource.GetResourcePath<AdvParticle>(fileName);

            var prefab = advEngine.Resource.Get<Sprite>(resourcePath);

            particlePlayer = UnityUtility.Instantiate<ParticlePlayer>(gameObject, prefab);

            if (sortingOrder.HasValue)
            {
                particlePlayer.SortingOrder = sortingOrder.Value;
            }

            return particlePlayer.Play(restart).AsUnitObservable();
        }

        #endif
    }
}
