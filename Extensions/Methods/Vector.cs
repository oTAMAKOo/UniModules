
using UnityEngine;
using System;
using System.Collections;

namespace UnityEngine
{
    public static class Vector
    {
        //====== Vector2 ======

        public static Vector2 SetX(Vector2 src, float val)
        {
            src.x = val;
            return src;
        }

        public static Vector2 SetY(Vector2 src, float val)
        {
            src.y = val;
            return src;
        }

        //====== Vector3 ======

        public static Vector3 SetX(Vector3 src, float val)
        {
            src.x = val;
            return src;
        }

        public static Vector3 SetY(Vector3 src, float val)
        {
            src.y = val;
            return src;
        }

        public static Vector3 SetZ(Vector3 src, float val)
        {
            src.z = val;
            return src;
        }

        //====== Vector4 ======

        public static Vector4 SetX(Vector4 src, float val)
        {
            src.x = val;
            return src;
        }

        public static Vector4 SetY(Vector4 src, float val)
        {
            src.y = val;
            return src;
        }

        public static Vector4 SetZ(Vector4 src, float val)
        {
            src.z = val;
            return src;
        }

        public static Vector4 SetW(Vector4 src, float val)
        {
            src.w = val;
            return src;
        }
    }
}
