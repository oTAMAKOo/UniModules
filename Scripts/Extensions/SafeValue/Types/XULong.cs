
using System;

namespace Extensions
{
    [Serializable]
    public struct XULong : IComparable, IComparable<ulong>, IConvertible, IEquatable<ulong>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(ulong)];

        private byte[] bytes;

        public XULong(ulong value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public ulong Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToUInt64(x, 0));
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        private static void UpdateValue(ulong value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(ulong)];
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

        public int CompareTo(ulong other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XULong other) { return Value == other.Value; }

        public bool Equals(ulong other) { return Value == other; }

        public override bool Equals(object other) { return other is XULong && Equals((XULong)other); }

        public override int GetHashCode() { return Value.GetHashCode(); }

        //--------------------------------------------------------
        // 文字列
        //--------------------------------------------------------

        public override string ToString() { return Value.ToString(); }

        public string ToString(IFormatProvider provider) { return Value.ToString(provider); }

        public string ToString(string format) { return Value.ToString(format); }

        public string ToString(string format, IFormatProvider provider) { return Value.ToString(format, provider); }

        //--------------------------------------------------------
        // 単項演算子
        //--------------------------------------------------------

        public static XULong operator ++(XULong v1) { return new XULong(v1.Value++); }

        public static XULong operator --(XULong v1) { return new XULong(v1.Value--); }

        public static bool operator true(XULong v1) { return v1.Value != 0; }

        public static bool operator false(XULong v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XULong operator +(XULong v1, XULong v2) { return new XULong(v1.Value + v2.Value); }

        public static XULong operator -(XULong v1, XULong v2) { return new XULong(v1.Value - v2.Value); }

        public static XULong operator *(XULong v1, XULong v2) { return new XULong(v1.Value * v2.Value); }

        public static XULong operator /(XULong v1, XULong v2) { return new XULong(v1.Value / v2.Value); }

        public static XULong operator %(XULong v1, XULong v2) { return new XULong(v1.Value % v2.Value); }

        public static XULong operator &(XULong v1, XULong v2) { return new XULong(v1.Value & v2.Value); }

        public static XULong operator |(XULong v1, XULong v2) { return new XULong(v1.Value | v2.Value); }

        public static XULong operator ^(XULong v1, XULong v2) { return new XULong(v1.Value ^ v2.Value); }

        public static XULong operator <<(XULong v1, int shift) { return new XULong(v1.Value << shift); }

        public static XULong operator >>(XULong v1, int shift) { return new XULong(v1.Value >> shift); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XULong v1, XULong v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XULong v1, XULong v2) { return v1.Value != v2.Value; }

        public static bool operator <(XULong v1, XULong v2) { return v1.Value < v2.Value; }

        public static bool operator >(XULong v1, XULong v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XULong v1, XULong v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XULong v1, XULong v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator ulong(XULong v) { return v.Value; }

        public static explicit operator XULong(ulong v) { return new XULong(v); }

        //------ From ulong to float, double ------

        public static implicit operator XFloat(XULong v) { return new XFloat(v.Value); }

        public static implicit operator XDouble(XULong v) { return new XDouble(v.Value); }

        //------ From ulong to sbyte, byte, short, ushort, int, uint, long, char ------

        public static explicit operator XULong(XSByte v) { return new XULong((ulong)v.Value); }

        public static explicit operator XULong(XByte v) { return new XULong(v.Value); }

        public static explicit operator XULong(XShort v) { return new XULong((ulong)v.Value); }

        public static explicit operator XULong(XUShort v) { return new XULong(v.Value); }

        public static explicit operator XULong(XInt v) { return new XULong((ulong)v.Value); }

        public static explicit operator XULong(XUInt v) { return new XULong(v.Value); }

        public static explicit operator XULong(XLong v) { return new XULong((ulong)v.Value); }

        public static explicit operator XULong(XChar v) { return new XULong(v.Value); }
    }
}