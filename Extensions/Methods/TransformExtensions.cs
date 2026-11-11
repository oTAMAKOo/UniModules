
using UnityEngine;
using System.Text;

namespace Extensions
{
    public static class TransformExtensions
    {
        public static string GetHierarchyPath(this Transform transform)
        {
            var pathBuilder = new StringBuilder();

            pathBuilder.Append(transform.gameObject.name);

            var parent = transform.parent;

            while (parent != null)
            {
                pathBuilder.Insert(0, "/");
                pathBuilder.Insert(0, parent.name);

                parent = parent.parent;
            }

            return pathBuilder.ToString();
        }

        public static void Copy(this Transform transform, Transform source)
        {
            transform.localPosition = source.localPosition;
            transform.localRotation = source.localRotation;
            transform.localScale = source.localScale;
        }

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
    }
}
