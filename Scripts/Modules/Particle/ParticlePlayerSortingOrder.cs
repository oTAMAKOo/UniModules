
using UnityEngine;
using Unity.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Particle
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticlePlayerSortingOrder : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private int sortingOrder = 0;

        private ParticleSystemRenderer particleSystemRenderer = null;

        //----- property -----

        public int SortingOrder { get { return sortingOrder; } }

        //----- method -----

        public void Set(int sortingOrder)
        {
            this.sortingOrder = sortingOrder;

            var particlePlayer = FindParentParticlePlayer();

            if (particlePlayer != null)
            {
                Apply(particlePlayer.SortingOrder);
            }
        }

        public void Apply(int baseSortingOrder)
        {
            var particleRenderer = GetParticleSystemRenderer();

            if (particleRenderer != null)
            {
                particleRenderer.sortingOrder = baseSortingOrder + sortingOrder;
            }
        }

        public ParticlePlayer FindParentParticlePlayer()
        {
            return gameObject.AncestorsAndSelf().OfComponent<ParticlePlayer>().FirstOrDefault();
        }

        private ParticleSystemRenderer GetParticleSystemRenderer()
        {
            return particleSystemRenderer ?? (particleSystemRenderer = UnityUtility.GetComponent<ParticleSystemRenderer>(gameObject));
        }
    }
}
