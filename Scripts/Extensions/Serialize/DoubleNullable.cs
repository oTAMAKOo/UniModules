﻿﻿
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class DoubleNullable : SerializableNullable<double>
    {
        public DoubleNullable(double value) : base(value) { }

        public DoubleNullable(double? value)
        {
            this.value = value.HasValue ? value.Value : default(double);
            this.hasValue = value.HasValue;
        }

        public static implicit operator DoubleNullable(double TValue) { return new DoubleNullable(TValue); }
        public static implicit operator DoubleNullable(double? TValue) { return new DoubleNullable(TValue); }
        public static implicit operator double? (DoubleNullable value) { return value.ToNullable(); }
        public double? ToNullable() { return HasValue ? new double?(Value) : new double?(); }
    }
}
