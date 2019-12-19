﻿﻿
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class LongNullable : SerializableNullable<long>
    {
        public LongNullable(long value) : base(value) { }

        public LongNullable(long? value)
        {
            this.value = value.HasValue ? value.Value : default(long);
            this.hasValue = value.HasValue;
        }

        public static implicit operator LongNullable(long TValue) { return new LongNullable(TValue); }
        public static implicit operator LongNullable(long? TValue) { return new LongNullable(TValue); }
        public static implicit operator long? (LongNullable value) { return value.ToNullable(); }
        public long? ToNullable() { return HasValue ? new long?(Value) : new long?(); }
    }
}
