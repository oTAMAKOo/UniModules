
using System;

namespace Extensions
{
	[Serializable]
	public struct XInt : IComparable, IComparable<int>, IConvertible, IEquatable<int>, IFormattable
	{
		private static readonly byte[] Buffer = new byte[sizeof(int)];

		private byte[] bytes;

		public XInt(int value)
		{
			bytes = new byte[0];

			SetVal(value, ref bytes);
		}

		public int Value
		{
			get
			{
				if (bytes == null) { return default; }

				lock (Buffer)
				{
					return SafeValue.UnPack(bytes, Buffer, x => BitConverter.ToInt32(x, 0));
				}
			}

			set { SetVal(value, ref bytes); }
		}

		private static void SetVal(int value, ref byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
			{
				bytes = new byte[sizeof(int)];
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

		public int CompareTo(int other) { return Value.CompareTo(other); }

		public int CompareTo(object obj) { return Value.CompareTo(obj); }

		public bool Equals(XInt other) { return Value == other.Value; }

		public bool Equals(int other) { return Value == other; }

		public override bool Equals(object other) { return other is XInt && Equals((XInt)other); }

		public override int GetHashCode() { return Value.GetHashCode(); }

		//--------------------------------------------------------
		// 文字列
		//--------------------------------------------------------

		public override string ToString() { return Value.ToString(); }

		public string ToString(IFormatProvider provider)  { return  Value.ToString(provider); }

		public string ToString(string format) { return Value.ToString(format); }

		public string ToString(string format, IFormatProvider provider) { return Value.ToString(format, provider); }

		//--------------------------------------------------------
		// 単項演算子
		//--------------------------------------------------------

		public static XInt operator ++(XInt v1) { return new XInt(v1.Value++); }

		public static XInt operator --(XInt v1) { return new XInt(v1.Value--); }

		public static bool operator true(XInt v1) { return v1.Value != 0; }

		public static bool operator false(XInt v1) { return v1.Value == 0; }

		//--------------------------------------------------------
		// 二項演算子
		//--------------------------------------------------------

		public static XInt operator +(XInt v1, XInt v2) { return new XInt(v1.Value + v2.Value); }

		public static XInt operator -(XInt v1, XInt v2) { return new XInt(v1.Value - v2.Value); }

		public static XInt operator *(XInt v1, XInt v2) { return new XInt(v1.Value * v2.Value); }

		public static XInt operator /(XInt v1, XInt v2) { return new XInt(v1.Value / v2.Value); }

		public static XInt operator %(XInt v1, XInt v2) { return new XInt(v1.Value % v2.Value); }

		public static XInt operator &(XInt v1, XInt v2) { return new XInt(v1.Value & v2.Value); }

		public static XInt operator |(XInt v1, XInt v2) { return new XInt(v1.Value | v2.Value); }

		public static XInt operator ^(XInt v1, XInt v2) { return new XInt(v1.Value ^ v2.Value); }

		public static XInt operator <<(XInt v1, int shift) { return new XInt(v1.Value << shift); }

		public static XInt operator >>(XInt v1, int shift) { return new XInt(v1.Value >> shift); }

		//--------------------------------------------------------
		// 比較演算子
		//--------------------------------------------------------

		public static bool operator ==(XInt v1, XInt v2) { return v1.Value == v2.Value; }

		public static bool operator !=(XInt v1, XInt v2) { return v1.Value != v2.Value; }

		public static bool operator <(XInt v1, XInt v2) { return v1.Value < v2.Value; }

		public static bool operator >(XInt v1, XInt v2) { return v1.Value > v2.Value; }

		public static bool operator <=(XInt v1, XInt v2) { return v1.Value <= v2.Value; }

		public static bool operator >=(XInt v1, XInt v2) { return v1.Value >= v2.Value; }

		//--------------------------------------------------------
		// 型変換演算
		//--------------------------------------------------------

		public static implicit operator int(XInt v) { return v.Value; }

		public static explicit operator XInt(int v) { return new XInt(v); }

		//------ From int to long, float, double ------

		public static implicit operator XLong(XInt v) { return new XLong(v.Value); }

		public static implicit operator XFloat(XInt v) { return new XFloat(v.Value); }

		public static implicit operator XDouble(XInt v) { return new XDouble(v.Value); }

		//------ From int to sbyte, byte, short, ushort, uint, ulong, char ------

		public static explicit operator XInt(XSByte v) { return new XInt(v.Value); }

		public static explicit operator XInt(XByte v) { return new XInt(v.Value); }

		public static explicit operator XInt(XShort v) { return new XInt(v.Value); }

		public static explicit operator XInt(XUShort v) { return new XInt(v.Value); }

		public static explicit operator XInt(XUInt v) { return new XInt((int)v.Value); }

		public static explicit operator XInt(XULong v) { return new XInt((int)v.Value); }

		public static explicit operator XInt(XChar v) { return new XInt(v.Value); }
	}
}