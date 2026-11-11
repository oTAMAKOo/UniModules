
using UnityEngine;

namespace Extensions
{
    public static class AnimatorExtensions
    {
        /// <summary> Animatorが利用可能か </summary>
        public static bool IsAvailable(this Animator self)
        {
            return self.gameObject.activeInHierarchy && self.runtimeAnimatorController != null;
        }
    }
}