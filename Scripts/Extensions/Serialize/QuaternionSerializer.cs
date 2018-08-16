﻿﻿﻿
using System;
using UnityEngine;

namespace Extensions.Serialize
{
    [Serializable]
    public class QuaternionSerializer
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public float w = 0f;

        public QuaternionSerializer() { }

        public QuaternionSerializer(float x = 0f, float y = 0f, float z = 0f, float w = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public QuaternionSerializer(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static explicit operator Quaternion(QuaternionSerializer val)
        {
            return val.ToQuaternion();
        }
    }
}