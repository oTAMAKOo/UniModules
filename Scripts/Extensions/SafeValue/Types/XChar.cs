
using System;

namespace Extensions
{
    [Serializable]
    public struct XChar : IComparable, IComparable<char>, IConvertible, IEquatable<char>, IFormattable
    {
        private static readonly byte[] Buffer = new byte[sizeof(char)];

        private byte[] bytes;

        public XChar(char value)
        {
            bytes = new byte[0];

            UpdateValue(value, ref bytes);
        }

        public char Value
        {
            get
            {
                if (bytes == null) { return default; }

                lock (Buffer)
                {
                    return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToChar(x, 0));
                }
            }

            set { UpdateValue(value, ref bytes); }
        }

        private static void UpdateValue(char value, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                bytes = new byte[sizeof(char)];
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

        public int CompareTo(char other) { return Value.CompareTo(other); }

        public int CompareTo(object obj) { return Value.CompareTo(obj); }

        public bool Equals(XChar other) { return Value == other.Value; }

        public bool Equals(char other) { return Value == other; }

        public override bool Equals(object other) { return other is XChar && Equals((XChar)other); }

        public override int GetHashCode() { return Value.GetHashCode(); }

        //--------------------------------------------------------
        // 文字列
        //--------------------------------------------------------

        public override string ToString() { return Value.ToString(); }

        public string ToString(IFormatProvider provider) { return Value.ToString(provider); }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            throw new NotSupportedException();
        }

        //--------------------------------------------------------
        // 単項演算子
        //--------------------------------------------------------

        public static XChar operator ++(XChar v1) { return new XChar((char)(v1.Value + 1)); }

        public static XChar operator --(XChar v1) { return new XChar((char)(v1.Value - 1)); }

        public static bool operator true(XChar v1) { return v1.Value != 0; }

        public static bool operator false(XChar v1) { return v1.Value == 0; }

        //--------------------------------------------------------
        // 比較演算子
        //--------------------------------------------------------

        public static bool operator ==(XChar v1, XChar v2) { return v1.Value == v2.Value; }

        public static bool operator !=(XChar v1, XChar v2) { return v1.Value != v2.Value; }

        public static bool operator <(XChar v1, XChar v2) { return v1.Value < v2.Value; }

        public static bool operator >(XChar v1, XChar v2) { return v1.Value > v2.Value; }

        public static bool operator <=(XChar v1, XChar v2) { return v1.Value <= v2.Value; }

        public static bool operator >=(XChar v1, XChar v2) { return v1.Value >= v2.Value; }

        //--------------------------------------------------------
        // 型変換演算
        //--------------------------------------------------------

        public static implicit operator char(XChar v) { return v.Value; }

        public static explicit operator XChar(char v) { return new XChar(v); }

        //------ From char to ushort, int, uint, long, ulong, float, double ------

        public static implicit operator XUShort(XChar v) { return new XUShort(v.Value); }

        public static implicit operator XInt(XChar v) { return new XInt(v.Value); }

        public static implicit operator XUInt(XChar v) { return new XUInt(v.Value); }

        public static implicit operator XLong(XChar v) { return new XLong(v.Value); }

        public static implicit operator XULong(XChar v) { return new XULong(v.Value); }
        
        public static implicit operator XFloat(XChar v) { return new XFloat(v.Value); }
        
        public static implicit operator XDouble(XChar v) { return new XDouble(v.Value); }
        
        //------ From char to sbyte, byte, short ------

        public static explicit operator XChar(XSByte v) { return new XChar((char)v.Value); }

        public static explicit operator XChar(XByte v) { return new XChar((char)v.Value); }

        public static explicit operator XChar(XShort v) { return new XChar((char)v.Value); }
    }
}