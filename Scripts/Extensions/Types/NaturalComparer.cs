
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Extensions
{
    //=================================================================
    // var labels = new string[0]; // Any text.
    // labels = labels.OrderBy(x => x, new NaturalComparer()).ToArray();
    //=================================================================

    /// <summary> 文字列自然順ソート用比較クラス </summary>
    public sealed class NaturalComparer : Comparer<string>, IDisposable
    {
        private Dictionary<string, string[]> table;

        public NaturalComparer()
        {
            table = new Dictionary<string, string[]>();
        }

        public void Dispose()
        {
            table.Clear();
            table = null;
        }

        public override int Compare(string x, string y)
        {
            if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(x)){ return 0; }

            if (x == y) { return 0; }

            string[] x1, y1;

            if (!table.TryGetValue(x, out x1))
            {
                x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)", RegexOptions.Compiled);
                table.Add(x, x1);
            }
            if (!table.TryGetValue(y, out y1))
            {
                y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)", RegexOptions.Compiled);
                table.Add(y, y1);
            }

            for (var i = 0; i < x1.Length && i < y1.Length; i++)
            {
                if (x1[i] != y1[i])
                {
                    return PartCompare(x1[i], y1[i]);
                }
            }
            if (y1.Length > x1.Length)
            {
                return 1;
            }
            else if (x1.Length > y1.Length)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        private static int PartCompare(string left, string right)
        {
            if (left.Length > right.Length)
            {
                return 1;
            }
            
            if (left.Length < right.Length)
            {
                return -1;
            }

            if (!long.TryParse(left, out var x))
            {
                return left.CompareTo(right);
            }

            if (!long.TryParse(right, out var y))
            {
                return left.CompareTo(right);
            }

            return x.CompareTo(y);
        }
    }
}
