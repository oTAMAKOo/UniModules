﻿﻿
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class DecimalNullable : SerializableNullable<decimal>
    {
        public DecimalNullable(decimal value) : base(value){}

        public DecimalNullable(decimal? value)
        {
            this.value = value.HasValue ? value.Value : default(decimal);
            this.hasValue = value.HasValue;
        }

        public static implicit operator DecimalNullable(decimal TValue) { return new DecimalNullable(TValue); }
        public static implicit operator DecimalNullable(decimal? TValue) { return new DecimalNullable(TValue); }
        public static implicit operator decimal? (DecimalNullable value) { return value.ToNullable(); }
        public decimal? ToNullable() { return HasValue ? new decimal?(Value) : new decimal?(); }
    }
}
