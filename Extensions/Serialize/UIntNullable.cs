
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public sealed class UIntNullable : SerializableNullable<uint>
    {
        public UIntNullable(uint value) : base(value) { }

        public UIntNullable(uint? value)
        {
            this.value = value.HasValue ? value.Value : default(uint);
            this.hasValue = value.HasValue;
        }

        public static implicit operator UIntNullable(uint TValue) { return new UIntNullable(TValue); }
        public static implicit operator UIntNullable(uint? TValue) { return new UIntNullable(TValue); }
        public static implicit operator uint? (UIntNullable value) { return value.ToNullable(); }
        public uint? ToNullable() { return HasValue ? new uint?(Value) : new uint?(); }
    }
}
