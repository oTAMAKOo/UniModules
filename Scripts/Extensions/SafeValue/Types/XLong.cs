
using System;

namespace Extensions
{
	[Serializable]
	public struct XLong : IComparable, IComparable<long>, IConvertible, IEquatable<long>, IFormattable
	{
		private static readonly byte[] Buffer = new byte[sizeof(long)];

		private byte[] bytes;

		public XLong(long value)
		{
			bytes = new byte[0];

			SetVal(value, ref bytes);
		}

		public long Value
		{
			get
			{
				if (bytes == null) { return default; }

				lock (Buffer)
				{
					return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToInt64(x, 0));
				}
			}

			set { SetVal(value, ref bytes); }
		}

		private static void SetVal(long value, ref byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				bytes = new byte[sizeof(long)];
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

		public int CompareTo(long other) { return Value.CompareTo(other); }

		public int CompareTo(object obj) { return Value.CompareTo(obj); }

		public bool Equals(XLong other) { return Value == other.Value; }

		public bool Equals(long other) { return Value == other; }

		public override bool Equals(object other) { return other is XLong && Equals((XLong)other); }

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

		public static XLong operator ++(XLong v1) { return new XLong(v1.Value++); }

		public static XLong operator --(XLong v1) { return new XLong(v1.Value--); }

		public static bool operator true(XLong v1) { return v1.Value != 0; }

		public static bool operator false(XLong v1) { return v1.Value == 0; }

		//--------------------------------------------------------
		// 二項演算子
		//--------------------------------------------------------

		public static XLong operator +(XLong v1, XLong v2) { return new XLong(v1.Value + v2.Value); }

		public static XLong operator -(XLong v1, XLong v2) { return new XLong(v1.Value - v2.Value); }

		public static XLong operator *(XLong v1, XLong v2) { return new XLong(v1.Value * v2.Value); }

		public static XLong operator /(XLong v1, XLong v2) { return new XLong(v1.Value / v2.Value); }

		public static XLong operator %(XLong v1, XLong v2) { return new XLong(v1.Value % v2.Value); }

		public static XLong operator &(XLong v1, XLong v2) { return new XLong(v1.Value & v2.Value); }

		public static XLong operator |(XLong v1, XLong v2) { return new XLong(v1.Value | v2.Value); }

		public static XLong operator ^(XLong v1, XLong v2) { return new XLong(v1.Value ^ v2.Value); }

		public static XLong operator <<(XLong v1, int shift) { return new XLong(v1.Value << shift); }

		public static XLong operator >>(XLong v1, int shift) { return new XLong(v1.Value >> shift); }

		//--------------------------------------------------------
		// 比較演算子
		//--------------------------------------------------------

		public static bool operator ==(XLong v1, XLong v2) { return v1.Value == v2.Value; }

		public static bool operator !=(XLong v1, XLong v2) { return v1.Value != v2.Value; }

		public static bool operator <(XLong v1, XLong v2) { return v1.Value < v2.Value; }

		public static bool operator >(XLong v1, XLong v2) { return v1.Value > v2.Value; }

		public static bool operator <=(XLong v1, XLong v2) { return v1.Value <= v2.Value; }

		public static bool operator >=(XLong v1, XLong v2) { return v1.Value >= v2.Value; }

		//--------------------------------------------------------
		// 型変換演算
		//--------------------------------------------------------

		public static implicit operator long(XLong v) { return v.Value; }

		public static explicit operator XLong(long v) { return new XLong(v); }

		//------ From long to float, double ------

		public static implicit operator XFloat(XLong v) { return new XFloat(v.Value); }

		public static implicit operator XDouble(XLong v) { return new XDouble(v.Value); }

		//------ From long to sbyte, byte, short, ushort, int, uint, ulong, char ------

		public static explicit operator XLong(XSByte v) { return new XLong(v.Value); }

		public static explicit operator XLong(XByte v) { return new XLong(v.Value); }

		public static explicit operator XLong(XShort v) { return new XLong(v.Value); }

		public static explicit operator XLong(XUShort v) { return new XLong(v.Value); }

		public static explicit operator XLong(XInt v) { return new XLong(v.Value); }

		public static explicit operator XLong(XUInt v) { return new XLong(v.Value); }

		public static explicit operator XLong(XULong v) { return new XLong((long)v.Value); }

		public static explicit operator XLong(XChar v) { return new XLong(v.Value); }
	}
}