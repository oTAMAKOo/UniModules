
using System;

namespace Extensions
{
    [Serializable]
    public struct XBool : IComparable, IComparable<bool>, IConvertible, IEquatable<bool>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(bool)];

        private byte[] bytes;

        public XBool(bool value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public bool Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToBoolean(x, 0));
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        private static void UpdateValue(bool value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(bool)];
            }

            SafeValue.Pack(ref bytes, () => BitConverter.GetBytes(value));
        }

        //--------------------------------------------------------
        // IConvertible
        //--------------------------------------------------------

        TypeCode IConvertible.GetTypeCode() { return Value.GetTypeCode(); }

        bool IConvertible.ToBoolean(IFormatProvider provider) { return Convert.ToBoolean(Value, provider); }

        byte IConvertible.ToByte(IFormatProvider provider) { return Convert.ToByte(Value, provider); }

        char IConvertible.ToChar(IFormatProvider provider) { return Convert.ToChar(Value, provider); }

        decimal IConvertible.ToDecimal(IFormatProvider provider) { return Convert.ToDecimal(Value, provider); }

        double IConvertible.ToDouble(IFormatProvider provider) { return Convert.ToDouble(Value, provider); }

        short IConvertible.ToInt16(IFormatProvider provider) { return Convert.ToInt16(Value, provider); }

        int IConvertible.ToInt32(IFormatProvider provider) { return Convert.ToInt32(Value, provider); }

        long IConvertible.ToInt64(IFormatProvider provider) { return Convert.ToInt64(Value, provider); }

        sbyte IConvertible.ToSByte(IFormatProvider provider) { return Convert.ToSByte(Value, provider); }

        float IConvertible.ToSingle(IFormatProvider provider) { return Convert.ToSingle(Value, provider); }

        ushort IConvertible.ToUInt16(IFormatProvider provider) { return Convert.ToUInt16(Value, provider); }

        uint IConvertible.ToUInt32(IFormatProvider provider) { return Convert.ToUInt32(Value, provider); }

        ulong IConvertible.ToUInt64(IFormatProvider provider) { return Convert.ToUInt64(Value, provider); }

        DateTime IConvertible.ToDateTime(IFormatProvider provider) { return Convert.ToDateTime(Value, provider); }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider) { return Convert.ChangeType(Value, conversionType, provider); }

        //--------------------------------------------------------
        // 比較
        //--------------------------------------------------------

        public int CompareTo(bool other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XBool other) { return Value == other.Value; }

        public bool Equals(bool other) { return Value == other; }

        public override bool Equals(object other) { return other is XBool && Equals((XBool)other); }

        public override int GetHashCode() { return Value.GetHashCode(); }

        //--------------------------------------------------------
        // 文字列
        //--------------------------------------------------------

        public override string ToString() { return Value.ToString(); }

        public string ToString(string format, IFormatProvider provider) { return Value.ToString(provider); }

        public string ToString(IFormatProvider provider) { return Value.ToString(provider); }

        //--------------------------------------------------------
        // 単項演算子
        //--------------------------------------------------------

        public static bool operator true(XBool v1) { return v1.Value; }

        public static bool operator false(XBool v1) { return v1.Value; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XBool operator |(XBool v1, XBool v2) { return new XBool(v1.Value | v2.Value); }

        public static XBool operator ^(XBool v1, XBool v2) { return new XBool(v1.Value ^ v2.Value); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XBool v1, XBool v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XBool v1, XBool v2) { return v1.Value != v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator bool(XBool v) { return v.Value; }

        public static explicit operator XBool(bool v) { return new XBool(v); }
    }
}