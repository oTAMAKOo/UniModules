﻿﻿
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class FloatNullable : SerializableNullable<float>
    {
        public FloatNullable(float value) : base(value) { }

        public FloatNullable(float? value)
        {
            this.value = value.HasValue ? value.Value : default(float);
            this.hasValue = value.HasValue;
        }

        public static implicit operator FloatNullable(float TValue) { return new FloatNullable(TValue); }
        public static implicit operator FloatNullable(float? TValue) { return new FloatNullable(TValue); }
        public static implicit operator float? (FloatNullable value) { return value.ToNullable(); }
        public float? ToNullable() { return HasValue ? new float?(Value) : new float?(); }
    }
}
