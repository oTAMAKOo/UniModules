
#if UNITY_EDITOR

using UnityEngine;

namespace Modules.Particle
{
    public partial class ParticlePlayer
    {
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

        [ContextMenu("CollectContents")]
        private void RunCollectContents()
        {
            CollectContents();
        }

        public virtual void Simulate(float time)
        {
            if (Application.isPlaying) { return; }

            if (State == State.Play)
            {
                foreach (var info in particleSystems)
                {
                    info.ParticleSystem.Simulate(time, true, false);
                }

                if (!IsAlive())
                {
                    ResetContents();
                }

                FrameUpdate(time);
            }
        }
    }
}

#endif