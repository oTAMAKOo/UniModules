
using System;

namespace Extensions
{
    [Serializable]
    public struct XSByte : IComparable, IComparable<sbyte>, IConvertible, IEquatable<sbyte>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(sbyte)];

        private byte[] bytes;

        public XSByte(sbyte value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public sbyte Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => (sbyte)x[0]);
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        private static void UpdateValue(sbyte value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(sbyte)];
            }

            SafeValue.Pack(ref bytes, () => new byte[]{ (byte)value });
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

        public int CompareTo(sbyte other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XSByte other) { return Value == other.Value; }

        public bool Equals(sbyte other) { return Value == other; }

        public override bool Equals(object other) { return other is XSByte && Equals((XSByte)other); }

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

        public static XSByte operator ++(XSByte v1) { return new XSByte((sbyte)(v1.Value + 1)); }

        public static XSByte operator --(XSByte v1) { return new XSByte((sbyte)(v1.Value - 1)); }

        public static bool operator true(XSByte v1) { return v1.Value != 0; }

        public static bool operator false(XSByte v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XSByte operator +(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value + v2.Value)); }

        public static XSByte operator -(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value - v2.Value)); }

        public static XSByte operator *(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value * v2.Value)); }

        public static XSByte operator /(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value / v2.Value)); }

        public static XSByte operator %(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value % v2.Value)); }

        public static XSByte operator &(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value & v2.Value)); }

        public static XSByte operator |(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value | v2.Value)); }

        public static XSByte operator ^(XSByte v1, XSByte v2) { return new XSByte((sbyte)(v1.Value ^ v2.Value)); }

        public static XSByte operator <<(XSByte v1, int shift) { return new XSByte((sbyte)(v1.Value << shift)); }

        public static XSByte operator >>(XSByte v1, int shift) { return new XSByte((sbyte)(v1.Value >> shift)); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XSByte v1, XSByte v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XSByte v1, XSByte v2) { return v1.Value != v2.Value; }

        public static bool operator <(XSByte v1, XSByte v2) { return v1.Value < v2.Value; }

        public static bool operator >(XSByte v1, XSByte v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XSByte v1, XSByte v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XSByte v1, XSByte v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator sbyte(XSByte v) { return v.Value; }

        public static explicit operator XSByte(sbyte v) { return new XSByte(v); }

        //------ From sbyte to short, int, long, float, double ------

        public static implicit operator XShort(XSByte v) { return new XShort(v.Value); }

        public static implicit operator XInt(XSByte v) { return new XInt(v.Value); }

        public static implicit operator XLong(XSByte v) { return new XLong(v.Value); }

        public static implicit operator XFloat(XSByte v) { return new XFloat(v.Value); }

        public static implicit operator XDouble(XSByte v) { return new XDouble(v.Value); }

        //------ From sbyte to byte, ushort, uint, ulong, char. ------

        public static explicit operator XSByte(XByte v) { return new XSByte((sbyte)v.Value); }

        public static explicit operator XSByte(XUShort v) { return new XSByte((sbyte)v.Value); }

        public static explicit operator XSByte(XUInt v) { return new XSByte((sbyte)v.Value); }

        public static explicit operator XSByte(XULong v) { return new XSByte((sbyte)v.Value); }

        public static explicit operator XSByte(XChar v) { return new XSByte((sbyte)v.Value); }
    }
}