
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

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

    }
}
