
using System;

namespace Extensions
{
	[Serializable]
	public struct XShort : IComparable, IComparable<short>, IConvertible, IEquatable<short>, IFormattable
	{
		private static readonly byte[] Buffer = new byte[sizeof(short)];

		private byte[] bytes;

		public XShort(short value)
		{
			bytes = new byte[0];

			SetVal(value, ref bytes);
		}

		public short Value
		{
			get
			{
				if (bytes == null) { return default; }

				lock (Buffer)
				{
					return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToInt16(x, 0));
				}
			}

			set { SetVal(value, ref bytes); }
		}

		private static void SetVal(short value, ref byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				bytes = new byte[sizeof(short)];
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

		public int CompareTo(short other) { return Value.CompareTo(other); }

		public int CompareTo(object obj) { return Value.CompareTo(obj); }

		public bool Equals(XShort other) { return Value == other.Value; }

		public bool Equals(short other) { return Value == other; }

		public override bool Equals(object other) { return other is XShort && Equals((XShort)other); }

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

		public static XShort operator ++(XShort v1) { return new XShort(v1.Value++); }

		public static XShort operator --(XShort v1) { return new XShort(v1.Value--); }

		public static bool operator true(XShort v1) { return v1.Value != 0; }

		public static bool operator false(XShort v1) { return v1.Value == 0; }

		//--------------------------------------------------------
		// 二項演算子
		//--------------------------------------------------------

		public static XShort operator +(XShort v1, XShort v2) { return new XShort((short)(v1.Value + v2.Value)); }

		public static XShort operator -(XShort v1, XShort v2) { return new XShort((short)(v1.Value - v2.Value)); }

		public static XShort operator *(XShort v1, XShort v2) { return new XShort((short)(v1.Value * v2.Value)); }

		public static XShort operator /(XShort v1, XShort v2) { return new XShort((short)(v1.Value / v2.Value)); }

		public static XShort operator %(XShort v1, XShort v2) { return new XShort((short)(v1.Value % v2.Value)); }

		public static XShort operator &(XShort v1, XShort v2) { return new XShort((short)(v1.Value & v2.Value)); }

		public static XShort operator |(XShort v1, XShort v2) { return new XShort((short)(v1.Value | v2.Value)); }

		public static XShort operator ^(XShort v1, XShort v2) { return new XShort((short)(v1.Value ^ v2.Value)); }

		public static XShort operator <<(XShort v1, int shift) { return new XShort((short)(v1.Value << shift)); }

		public static XShort operator >>(XShort v1, int shift) { return new XShort((short)(v1.Value >> shift)); }

		//--------------------------------------------------------
		// 比較演算子
		//--------------------------------------------------------

		public static bool operator ==(XShort v1, XShort v2) { return v1.Value == v2.Value; }

		public static bool operator !=(XShort v1, XShort v2) { return v1.Value != v2.Value; }

		public static bool operator <(XShort v1, XShort v2) { return v1.Value < v2.Value; }

		public static bool operator >(XShort v1, XShort v2) { return v1.Value > v2.Value; }

		public static bool operator <=(XShort v1, XShort v2) { return v1.Value <= v2.Value; }

		public static bool operator >=(XShort v1, XShort v2) { return v1.Value >= v2.Value; }

		//--------------------------------------------------------
		// 型変換演算
		//--------------------------------------------------------

		public static implicit operator short(XShort v) { return v.Value; }

		public static explicit operator XShort(short v) { return new XShort(v); }

		//------ From short to int, long, float, double ------

		public static implicit operator XInt(XShort v) { return new XInt(v.Value); }

		public static implicit operator XLong(XShort v) { return new XLong(v.Value); }

		public static implicit operator XFloat(XShort v) { return new XFloat(v.Value); }

		public static implicit operator XDouble(XShort v) { return new XDouble(v.Value); }

		//------ From short to sbyte, byte, ushort, uint, ulong, char ------

		public static explicit operator XShort(XSByte v) { return new XShort(v.Value); }

		public static explicit operator XShort(XByte v) { return new XShort(v.Value); }

		public static explicit operator XShort(XUShort v) { return new XShort((short)v.Value); }

		public static explicit operator XShort(XUInt v) { return new XShort((short)v.Value); }

		public static explicit operator XShort(XULong v) { return new XShort((short)v.Value); }

		public static explicit operator XShort(XChar v) { return new XShort((short)v.Value); }
	}
}