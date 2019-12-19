﻿﻿
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class IntNullable : SerializableNullable<int>
    {
        public IntNullable(int value) : base(value) { }

        public IntNullable(int? value)
        {
            this.value = value.HasValue ? value.Value : default(int);
            this.hasValue = value.HasValue;
        }

        public static implicit operator IntNullable(int TValue) { return new IntNullable(TValue); }
        public static implicit operator IntNullable(int? TValue) { return new IntNullable(TValue); }
        public static implicit operator int? (IntNullable value) { return value.ToNullable(); }
        public int? ToNullable() { return HasValue ? new int?(Value) : new int?(); }
    }
}
