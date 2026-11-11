
using System;

namespace Extensions
{
    [Serializable]
    public struct XUShort : IComparable, IComparable<ushort>, IConvertible, IEquatable<ushort>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(ushort)];

        private byte[] bytes;

        public XUShort(ushort value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public ushort Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToUInt16(x, 0));
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        private static void UpdateValue(ushort value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(ushort)];
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

        public int CompareTo(ushort other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XUShort other) { return Value == other.Value; }

        public bool Equals(ushort other) { return Value == other; }

        public override bool Equals(object other) { return other is XUShort && Equals((XUShort)other); }

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

        public static XUShort operator ++(XUShort v1) { return new XUShort((ushort)(v1.Value + 1)); }

        public static XUShort operator --(XUShort v1) { return new XUShort((ushort)(v1.Value - 1)); }

        public static bool operator true(XUShort v1) { return v1.Value != 0; }

        public static bool operator false(XUShort v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XUShort operator +(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value + v2.Value)); }

        public static XUShort operator -(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value - v2.Value)); }

        public static XUShort operator *(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value * v2.Value)); }

        public static XUShort operator /(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value / v2.Value)); }

        public static XUShort operator %(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value % v2.Value)); }

        public static XUShort operator &(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value & v2.Value)); }

        public static XUShort operator |(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value | v2.Value)); }

        public static XUShort operator ^(XUShort v1, XUShort v2) { return new XUShort((ushort)(v1.Value ^ v2.Value)); }

        public static XUShort operator <<(XUShort v1, int shift) { return new XUShort((ushort)(v1.Value << shift)); }

        public static XUShort operator >>(XUShort v1, int shift) { return new XUShort((ushort)(v1.Value >> shift)); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XUShort v1, XUShort v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XUShort v1, XUShort v2) { return v1.Value != v2.Value; }

        public static bool operator <(XUShort v1, XUShort v2) { return v1.Value < v2.Value; }

        public static bool operator >(XUShort v1, XUShort v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XUShort v1, XUShort v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XUShort v1, XUShort v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator ushort(XUShort v) { return v.Value; }

        public static explicit operator XUShort(ushort v) { return new XUShort(v); }

        //------ From ushort to int, uint, long, ulong, float, double ------

        public static implicit operator XInt(XUShort v) { return new XInt(v.Value); }

        public static implicit operator XUInt(XUShort v) { return new XUInt(v.Value); }

        public static implicit operator XLong(XUShort v) { return new XLong(v.Value); }

        public static implicit operator XULong(XUShort v) { return new XULong(v.Value); }

        public static implicit operator XFloat(XUShort v) { return new XFloat(v.Value); }

        public static implicit operator XDouble(XUShort v) { return new XDouble(v.Value); }

        //------ From ushort to sbyte, byte, short, char ------

        public static explicit operator XUShort(XSByte v) { return new XUShort((ushort)v.Value); }
        
        public static explicit operator XUShort(XByte v) { return new XUShort(v.Value); }

        public static explicit operator XUShort(XShort v) { return new XUShort((ushort)v.Value); }

        public static explicit operator XUShort(XChar v) { return new XUShort(v.Value); }
    }
}