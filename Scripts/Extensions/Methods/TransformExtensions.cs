
using UnityEngine;

namespace Extensions
{
    public static class TransformExtensions
    {
        public static void Reset(this Transform transform, bool localPosition = true, bool localRotation = true, bool localScale = true)
        {
            if (localPosition)
            {
                transform.localPosition = Vector3.zero;
            }

            if (localRotation)
            {
                transform.localRotation = Quaternion.identity;
            }

            if (localScale)
            {
                transform.localScale = Vector3.one;
            }
        }

        public static void Copy(this Transform transform, Transform source)
        {
            transform.localPosition = source.localPosition;
            transform.localRotation = source.localRotation;
            transform.localScale = source.localScale;
        }
    }
}
