
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Extensions.Serialize;

namespace Extensions
{
    public static partial class StringExtensions
    {
        /// <summary> SringBuilderを使って複数の文字列を連結. </summary>
        public static string Combine(this string value, string[] targets)
        {
            var builder = new StringBuilder(value);

            foreach (var target in targets)
            {
                if (string.IsNullOrEmpty(target)) { continue; }

                builder.Append(target);
            }

            return builder.ToString();
        }

        /// <summary> 改行コードを統一 </summary>
        public static string FixLineEnd(this string value, string newLineStr = "\n")
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            var regex = new Regex(@"\r|\r\n");

            if (regex.IsMatch(value))
            {
                value = value.Replace("\r\n", "\r").Replace("\r", newLineStr);
            }

            return value;
        }

        /// <summary> エスケープシーケンス(\t、\nなど)を制御コード(\\t、\\n)に変換 </summary>
        public static string Escape(this string value)
        {
            return Regex.Escape(value);
        }

        /// <summary> 制御コード(\\t、\\n)をエスケープシーケンス(\t、\nなど)に変換 </summary>
        public static string Unescape(this string value)
        {
            return Regex.Unescape(value);
        }

        /// <summary> 指定された文字列が指定範囲と一致するか判定 </summary>
        public static bool SubstringEquals(this string value, int startIndex, int length, string target)
        {
            if (value.Length < startIndex + length) { return false; }

            return value.SafeSubstring(startIndex, length) == target;
        }

        /// <summary>
        /// このインスタンスから部分文字列を取得します.
        /// 部分文字列は、文字列中の指定した文字の位置で開始し、文字列の末尾まで続きます.
        /// </summary>
        public static string SafeSubstring(this string value, int startIndex, int? length = null)
        {
            var len = length.HasValue ? length.Value : int.MaxValue;

            return new string((value ?? string.Empty).Skip(startIndex).Take(len).ToArray());
        }

        /// <summary> 指定された文字列をSHA256でハッシュ化 </summary>
        public static string GetHash(this string value)
        {
            return CalcSHA256(value, Encoding.UTF8);
        }

        /// <summary> 指定された文字列をSHA256でハッシュ化 </summary>
        public static string GetHash(this string value, Encoding enc)
        {
            return CalcSHA256(value, enc);
        }

        // SHA256ハッシュ生成.
        private static string CalcSHA256(string value, Encoding enc)
        {
            var byteValues = enc.GetBytes(value);

            var crypt256 = new SHA256CryptoServiceProvider();

            var hash256Value = crypt256.ComputeHash(byteValues);
            
            var hashedText = new StringBuilder();

            for (var i = 0; i < hash256Value.Length; i++)
            {
                hashedText.AppendFormat("{0:x2}", hash256Value[i]);
            }

            return hashedText.ToString();
        }

        /// <summary> 指定された文字列をCRC32でハッシュ化 </summary>
        public static string GetCRC(this string value)
        {
            return CalcCRC32(value, Encoding.UTF8);
        }

        /// <summary> 指定された文字列をCRC32でハッシュ化 </summary>
        public static string GetCRC(this string value, Encoding enc)
        {
            return CalcCRC32(value, enc);
        }

        // CRC32ハッシュ生成.
        private static string CalcCRC32(string value, Encoding enc)
        {
            var byteValues = enc.GetBytes(value);

            var crc32 = new CRC32();

            var hashValue = crc32.ComputeHash(byteValues);

            var hashedText = new StringBuilder();

            for (var i = 0; i < hashValue.Length; i++)
            {
                hashedText.AppendFormat("{0:x2}", hashValue[i]);
            }

            return hashedText.ToString();
        }

        /// <summary> 文字列に指定されたキーワード群が含まれるか判定 </summary>
        public static bool IsMatch(this string text, string[] keywords)
        {
            keywords = keywords.Select(x => x.ToLower()).ToArray();

            if (!string.IsNullOrEmpty(text))
            {
                var tl = text.ToLower();
                var matches = 0;

                for (var b = 0; b < keywords.Length; ++b)
                {
                    if (tl.Contains(keywords[b])) ++matches;
                }

                if (matches == keywords.Length)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary> タグ文字列を除外した文字列を返す </summary>
        public static string RemoveTag(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        /// <summary> 文字列を指定された型に変換 </summary>
        public static T To<T>(this string value, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(value)) { return defaultValue; }

            var type = typeof(T);
            object result = null;

            try
            {
                if (type == typeof(string)) { result = value; }
                // Nullable型.
                else if (type == typeof(IntNullable))
                {
                    result = string.IsNullOrEmpty(value) ? new IntNullable(null) : new IntNullable(int.Parse(value));
                }
                else if (type == typeof(FloatNullable))
                {
                    result = string.IsNullOrEmpty(value) ? new FloatNullable(null) : new FloatNullable(float.Parse(value));
                }
                // 整数型.
                else if (type == typeof(sbyte)) { result = sbyte.Parse(value); }
                else if (type == typeof(byte)) { result = byte.Parse(value); }
                else if (type == typeof(char)) { result = char.Parse(value); }
                else if (type == typeof(short)) { result = short.Parse(value); }
                else if (type == typeof(ushort)) { result = ushort.Parse(value); }
                else if (type == typeof(int)) { result = int.Parse(value); }
                else if (type == typeof(uint)) { result = uint.Parse(value); }
                else if (type == typeof(long)) { result = long.Parse(value); }
                else if (type == typeof(ulong)) { result = ulong.Parse(value); }
                // 浮動小数点型.
                else if (type == typeof(float)) { result = float.Parse(value); }
                else if (type == typeof(double)) { result = double.Parse(value); }
                // その他.
                else if (type == typeof(decimal)) { result = decimal.Parse(value); }
                else if (type == typeof(DateTime)) { result = new DateTime(DateTime.Parse(value).Ticks, DateTimeKind.Utc); }
                else if (type.BaseType == typeof(Enum)) { result = Enum.Parse(type, value); }
                else if (type == typeof(bool))
                {
                    if (value == "0" || value == "1")
                    {
                        result = value == "1";
                    }
                    else
                    {
                        result = bool.Parse(value);
                    }
                }
                else
                {
                    throw new InvalidCastException(string.Format("Unknown type {0} (= {1}) is used.", type.FullName, value));
                }
            }
            catch (Exception e)
            {
                if (e is FormatException)
                {
                    throw new ArgumentException(string.Format("Defined type and value type do not match.\nparam ={0} value={1}", value, type.FullName));
                }

                return defaultValue;
            }

            return (T)result;
        }
    }
}
