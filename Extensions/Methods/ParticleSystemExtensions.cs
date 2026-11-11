
using System.Collections.Generic;
using UnityEngine;

namespace Extensions
{
    public static class ParticleSystemExtensions
    {
        /// <summary> 再生状態か取得. </summary>
        public static bool IsPlayback(this ParticleSystem particleSystem, bool subemitter = false)
        {
            // ※ ParticleSystem.IsAlive()は常にtrueを返すバグがあるので使わない.

            if (UnityUtility.IsNull(particleSystem)) { return false; }

            if (!UnityUtility.IsActiveInHierarchy(particleSystem)) { return false; }

            // ループエフェクトは常に生存.
            if (particleSystem.main.loop) { return true; }

            // サブエミッターはtimeが更新されないので再生時間で判定しない.
            if (!subemitter)
            {
                // 再生時間より短いか.
                if (particleSystem.time < particleSystem.main.duration) { return true; }
            }

            // 1つでも生きてるParticleSystemがいたら生存中.
            if (0 < particleSystem.particleCount) { return true; }

            return false;
        }

        /// <summary> サブエミッター一覧取得. </summary>
        public static ParticleSystem[] GetSubemitters(this ParticleSystem particleSystem)
        {
            if (!particleSystem.subEmitters.enabled) { return new ParticleSystem[0]; }

            var list = new List<ParticleSystem>();

            for (var i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
            {
                list.Add(particleSystem.subEmitters.GetSubEmitterSystem(i));
            }

            return list.ToArray();
        }
    }
}
