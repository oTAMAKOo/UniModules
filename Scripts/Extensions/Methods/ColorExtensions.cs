
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Extensions
{
    public static class ColorExtensions
    {
        public static string ColorToHex(this Color color, bool hasAlpha = false)
        {
            return ColorToHex((Color32)color, hasAlpha);
        }

        public static string ColorToHex(this Color32 color, bool hasAlpha = false)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");

            if (hasAlpha)
            {
                hex += color.a.ToString("X2");
            }

            return hex;
        }

        public static Color HexToColor(this string hex)
        {
            hex = hex.Replace("0x", "");
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return new Color32(r, g, b, a);
        }

        /// <summary> 文字列から一意のカラーコードを生成 </summary>
        public static string ToHexCode(this string str)
        {
            return str.ToHexCode(Encoding.UTF8);
        }

        /// <summary> 文字列から一意のカラーコードを生成 </summary>
        public static string ToHexCode(this string str, Encoding enc)
        {
            var crc = str.GetCRC();

            var bytes = enc.GetBytes(crc);

            var r = (bytes[0] & 0xFF) % 100;
            var g = (bytes[1] & 0xFF) % 100;
            var b = (bytes[2] & 0xFF) % 100;

            return string.Format("0x{0:D2}{1:D2}{2:D2}", r, g, b);
        }
    }
}
