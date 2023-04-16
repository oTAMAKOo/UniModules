
using System;
using System.Text;

namespace Extensions
{
	public struct XString
	{
		private static readonly Encoding UTF8Encoding = Encoding.UTF8;

		private byte[] bytes;

		private byte[] buffer;

		public XString(string value)
		{
			bytes = new byte[0];
			buffer = new byte[0];

			SetValue(value, ref bytes);
		}

		public string Value
		{
			get
			{
				if (bytes == null) { return default; }

				buffer = new byte[bytes.Length];

				return SafeValue.UnPack(bytes, buffer, x => UTF8Encoding.GetString(x));
			}

			set
			{
				SetValue(value, ref bytes);
			}
		}

		private static void SetValue(string value, ref byte[] bytes)
		{
			var size = UTF8Encoding.GetByteCount(value);

			bytes = new byte[size];

			SafeValue.Pack(ref bytes, () => UTF8Encoding.GetBytes(value));
		}

		//--------------------------------------------------------
		// 比較
		//--------------------------------------------------------

		public bool Equals(XString other) { return Value == other.Value; }

		public override bool Equals(object other) { return other is XString && Equals((XString)other); }

		public override int GetHashCode() { return Value.GetHashCode(); }

		//--------------------------------------------------------
		// 文字列
		//--------------------------------------------------------

		public override string ToString() { return Value; }

		public string ToString(IFormatProvider provider) { return Value.ToString(provider); }

		//--------------------------------------------------------
		// 比較演算子
		//--------------------------------------------------------

		public static bool operator ==(XString v1, XString v2) { return v1.Value == v2.Value; }

		public static bool operator !=(XString v1, XString v2) { return v1.Value != v2.Value; }

		//--------------------------------------------------------
		// 型変換演算
		//--------------------------------------------------------

		public static implicit operator string(XString v) { return v.Value; }

		public static explicit operator XString(string v) { return new XString(v); }
	}
}