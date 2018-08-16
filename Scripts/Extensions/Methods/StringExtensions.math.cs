

using System;
using System.Linq;
using System.Collections.Generic;
using org.mariuszgromada.math.mxparser;

//======================================================
// mXparser – Math Expressions 
// URL: http://mathparser.org/mxparser-tutorial/
//======================================================

namespace Extensions
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// 文字列の式を評価.
        /// 例) formula : "1+2*(3+4)+5*(6*(7+8)+(9+10))"
        /// </summary>
        public static double? MathEval(this string formula)
        {
            try
            {
                var expression = new Expression(formula);

                return expression.calculate();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 文字列の式を評価.
        /// 例) formula : "A1+A2*(B3+C4)"
        ///     arguments : new Dictionary(){ "A1" = 1, "A2" = 10, "B3" = 20, "C4" = 5 }
        /// </summary>
        public static double? MathEval(this string formula, Dictionary<string, double> arguments)
        {
            try
            {
                var args = new List<Argument>();

                foreach (var argument in arguments)
                {
                    var arg = new Argument(argument.Key, argument.Value);

                    args.Add(arg);
                }

                var expression = new Expression(formula, args.ToArray());

                return expression.calculate();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
