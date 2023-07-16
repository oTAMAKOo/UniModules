
using System;

namespace Extensions
{
    [Serializable]
    public struct XFloat : IComparable, IComparable<float>, IConvertible, IEquatable<float>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(float)];

        private byte[] bytes;

        public XFloat(float value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public float Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToSingle(x, 0));
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        public void SetValue(float value)
        {
            UpdateValue(value, ref bytes);
        }

        private static void UpdateValue(float value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(float)];
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

        public int CompareTo(float other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XFloat other) { return Value == other.Value; }

        public bool Equals(float other) { return Value == other; }

        public override bool Equals(object other) { return other is XFloat && Equals((XFloat)other); }

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

        public static XFloat operator ++(XFloat v1) { return new XFloat(v1.Value++); }

        public static XFloat operator --(XFloat v1) { return new XFloat(v1.Value--); }

        public static bool operator true(XFloat v1) { return v1.Value != 0; }

        public static bool operator false(XFloat v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 二項演算子
        //--------------------------------------------------------

        public static XFloat operator +(XFloat v1, XFloat v2) { return new XFloat(v1.Value + v2.Value); }

        public static XFloat operator -(XFloat v1, XFloat v2) { return new XFloat(v1.Value - v2.Value); }

        public static XFloat operator *(XFloat v1, XFloat v2) { return new XFloat(v1.Value * v2.Value); }

        public static XFloat operator /(XFloat v1, XFloat v2) { return new XFloat(v1.Value / v2.Value); }

        public static XFloat operator %(XFloat v1, XFloat v2) { return new XFloat(v1.Value % v2.Value); }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XFloat v1, XFloat v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XFloat v1, XFloat v2) { return v1.Value != v2.Value; }

        public static bool operator <(XFloat v1, XFloat v2) { return v1.Value < v2.Value; }

        public static bool operator >(XFloat v1, XFloat v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XFloat v1, XFloat v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XFloat v1, XFloat v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator float(XFloat v) { return v.Value; }

        public static explicit operator XFloat(float v) { return new XFloat(v); }

        //------ From float to double ------

        public static implicit operator XDouble(XFloat v) { return new XDouble(v.Value); }

        //------ From float to sbyte, byte, short, ushort, int, uint, long, ulong, char ------

        public static explicit operator XFloat(XSByte v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XByte v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XShort v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XUShort v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XInt v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XUInt v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XLong v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XULong v) { return new XFloat(v.Value); }

        public static explicit operator XFloat(XChar v) { return new XFloat(v.Value); }
    }
}