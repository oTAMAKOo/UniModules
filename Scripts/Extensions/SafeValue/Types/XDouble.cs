
using System;

namespace Extensions
{
	[Serializable]
	public struct XDouble : IComparable, IComparable<double>, IConvertible, IEquatable<double>, IFormattable
	{
		private static readonly byte[] Buffer = new byte[sizeof(double)];

		private byte[] bytes;

		public XDouble(double value)
		{
			bytes = new byte[0];

			SetVal(value, ref bytes);
		}

		public double Value
		{
			get
			{
				if (bytes == null) { return default; }

				lock (Buffer)
				{
					return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToDouble(x, 0));
				}
			}

			set { SetVal(value, ref bytes); }
		}

		private static void SetVal(double value, ref byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				bytes = new byte[sizeof(double)];
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

		public int CompareTo(double other) { return Value.CompareTo(other); }

		public int CompareTo(object obj) { return Value.CompareTo(obj); }

		public bool Equals(XDouble other) { return Value == other.Value; }

		public bool Equals(double other) { return Value == other; }

		public override bool Equals(object other) { return other is XDouble && Equals((XDouble)other); }

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

		public static XDouble operator ++(XDouble v1) { return new XDouble(v1.Value++); }

		public static XDouble operator --(XDouble v1) { return new XDouble(v1.Value--); }

		public static bool operator true(XDouble v1) { return v1.Value != 0; }

		public static bool operator false(XDouble v1) { return v1.Value == 0; }

		//--------------------------------------------------------
		// 二項演算子
		//--------------------------------------------------------

		public static XDouble operator +(XDouble v1, XDouble v2) { return new XDouble(v1.Value + v2.Value); }

		public static XDouble operator -(XDouble v1, XDouble v2) { return new XDouble(v1.Value - v2.Value); }

		public static XDouble operator *(XDouble v1, XDouble v2) { return new XDouble(v1.Value * v2.Value); }

		public static XDouble operator /(XDouble v1, XDouble v2) { return new XDouble(v1.Value / v2.Value); }

		public static XDouble operator %(XDouble v1, XDouble v2) { return new XDouble(v1.Value % v2.Value); }

		//--------------------------------------------------------
		// 比較演算子
		//--------------------------------------------------------

		public static bool operator ==(XDouble v1, XDouble v2) { return v1.Value == v2.Value; }

		public static bool operator !=(XDouble v1, XDouble v2) { return v1.Value != v2.Value; }

		public static bool operator <(XDouble v1, XDouble v2) { return v1.Value < v2.Value; }

		public static bool operator >(XDouble v1, XDouble v2) { return v1.Value > v2.Value; }

		public static bool operator <=(XDouble v1, XDouble v2) { return v1.Value <= v2.Value; }

		public static bool operator >=(XDouble v1, XDouble v2) { return v1.Value >= v2.Value; }

		//--------------------------------------------------------
		// 型変換演算
		//--------------------------------------------------------

		public static implicit operator double(XDouble v) { return v.Value; }

		public static explicit operator XDouble(double v) { return new XDouble(v); }

		//------ From double to sbyte, byte, short, ushort, int, uint, long, ulong, char, float ------

		public static explicit operator XDouble(XSByte v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XByte v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XShort v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XUShort v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XInt v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XUInt v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XLong v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XULong v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XChar v) { return new XDouble(v.Value); }

		public static explicit operator XDouble(XFloat v) { return new XDouble(v.Value); }

	}
}