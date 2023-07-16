
using System;

namespace Extensions
{
    [Serializable]
    public struct XByte : IComparable, IComparable<byte>, IConvertible, IEquatable<byte>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(byte)];

        private byte[] bytes;

        public XByte(byte value)
        {
            bytes = new byte[0];

            SetVal(value, ref bytes);
        }

        public byte Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => x[0]);
                }
            }

            set { SetVal(value, ref bytes); }
        }

        public static void SetVal(byte value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(byte)];
            }

            SafeValue.Pack(ref bytes, () => new byte[] { value });
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

        public int CompareTo(byte other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XByte other) { return Value == other.Value; }

        public bool Equals(byte other) { return Value == other; }

        public override bool Equals(object other) { return other is XByte && Equals((XByte)other); }

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

        public static XByte operator ++(XByte v1) { return new XByte(v1.Value++); }

        public static XByte operator --(XByte v1) { return new XByte(v1.Value--); }

        public static bool operator true(XByte v1) { return v1.Value != 0; }

        public static bool operator false(XByte v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XByte operator +(XByte v1, XByte v2) { return new XByte((byte)(v1.Value + v2.Value)); }

        public static XByte operator -(XByte v1, XByte v2) { return new XByte((byte)(v1.Value - v2.Value)); }

        public static XByte operator *(XByte v1, XByte v2) { return new XByte((byte)(v1.Value * v2.Value)); }

        public static XByte operator /(XByte v1, XByte v2) { return new XByte((byte)(v1.Value / v2.Value)); }

        public static XByte operator %(XByte v1, XByte v2) { return new XByte((byte)(v1.Value % v2.Value)); }

        public static XByte operator &(XByte v1, XByte v2) { return new XByte((byte)(v1.Value & v2.Value)); }

        public static XByte operator |(XByte v1, XByte v2) { return new XByte((byte)(v1.Value | v2.Value)); }

        public static XByte operator ^(XByte v1, XByte v2) { return new XByte((byte)(v1.Value ^ v2.Value)); }

        public static XByte operator <<(XByte v1, int shift) { return new XByte((byte)(v1.Value << shift)); }

        public static XByte operator >>(XByte v1, int shift) { return new XByte((byte)(v1.Value >> shift)); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XByte v1, XByte v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XByte v1, XByte v2) { return v1.Value != v2.Value; }

        public static bool operator <(XByte v1, XByte v2) { return v1.Value < v2.Value; }

        public static bool operator >(XByte v1, XByte v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XByte v1, XByte v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XByte v1, XByte v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator byte(XByte v) { return v.Value; }

        public static explicit operator XByte(byte v) { return new XByte(v); }

        //------ From byte to short, ushort, int, uint, long, ulong, float, double ------

        public static implicit operator XShort(XByte v) { return new XShort(v.Value); }

        public static implicit operator XUShort(XByte v) { return new XUShort(v.Value); }

        public static implicit operator XInt(XByte v) { return new XInt(v.Value); }

        public static implicit operator XUInt(XByte v) { return new XUInt(v.Value); }

        public static implicit operator XLong(XByte v) { return new XLong(v.Value); }

        public static implicit operator XULong(XByte v) { return new XULong(v.Value); }

        public static implicit operator XFloat(XByte v) { return new XFloat(v.Value); }

        public static implicit operator XDouble(XByte v) { return new XDouble(v.Value); }

        //------ From byte to sbyte, char. ------

        public static explicit operator XByte(XSByte v) { return new XByte((byte)v.Value); }

        public static explicit operator XByte(XChar v) { return new XByte((byte)v.Value); }
    }
}