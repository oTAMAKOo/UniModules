
using UnityEngine;

namespace Extensions
{
    public static class GameObjectExtensions
    {
        public static void ResetTransform(this GameObject gameObject, bool localPosition = true, bool localRotation = true, bool localScale = true)
        {
            gameObject.transform.Reset(localPosition, localRotation, localScale);
        }
    }
}
