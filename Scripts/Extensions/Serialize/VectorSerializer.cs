﻿﻿﻿﻿
using UnityEngine;
using System;

namespace Extensions.Serialize
{
    [Serializable]
    public class Vector2Serializer
    {
        public float x = 0f;
        public float y = 0f;

        public Vector2Serializer() { }

        public Vector2Serializer(float x = 0f, float y = 0f)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Serializer(Vector2 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }

        public static explicit operator Vector2(Vector2Serializer val)
        {
            return val.ToVector2();
        }
    }

    [Serializable]
    public class Vector3Serializer
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;

        public Vector3Serializer() { }

        public Vector3Serializer(float x = 0f, float y = 0f, float z = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Serializer(Vector3 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
            this.z = vec.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static explicit operator Vector3(Vector3Serializer val)
        {
            return val.ToVector3();
        }
    }

    [Serializable]
    public class Vector4Serializer
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public float w = 0f;

        public Vector4Serializer() { }

        public Vector4Serializer(float x = 0f, float y = 0f, float z = 0f, float w = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4Serializer(Vector4 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
            this.z = vec.z;
            this.w = vec.w;
        }

        public Vector4 ToVector4()
        {
            return new Vector4(x, y, z, w);
        }

        public static explicit operator Vector4(Vector4Serializer val)
        {
            return val.ToVector4();
        }
    }
}