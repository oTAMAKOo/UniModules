
using System;

namespace Extensions
{
    /// <summary>
    /// 正規分布の値を生成.
    ///
    /// Box-Muller法.
    /// https://ja.wikipedia.org/wiki/%E3%83%9C%E3%83%83%E3%82%AF%E3%82%B9%EF%BC%9D%E3%83%9F%E3%83%A5%E3%83%A9%E3%83%BC%E6%B3%95
    /// </summary>
    public sealed class RandomBoxMuller
    {
        public double Get(double mu = 0.0, double sigma = 1.0, bool getCos = true)
        {
            var rand1 = RandomUtility.RandomInRange(0.0, 1.0);
            var rand2 = RandomUtility.RandomInRange(0.0, 1.0);

            var normrand = 0.0;

            if (getCos)
            {
                normrand = Math.Sqrt(-2.0 * Math.Log(rand1)) * Math.Cos(2.0 * Math.PI * rand2);
            }
            else
            {
                normrand = Math.Sqrt(-2.0 * Math.Log(rand1)) * Math.Sin(2.0 * Math.PI * rand2);
            }

            normrand = normrand * sigma + mu;

            return normrand;
        }

        public double[] GetPair(double mu = 0.0, double sigma = 1.0)
        {
            var normrand = new double[2];

            var rand1 = RandomUtility.RandomInRange(0.0, 1.0);
            var rand2 = RandomUtility.RandomInRange(0.0, 1.0);

            normrand[0] = Math.Sqrt(-2.0 * Math.Log(rand1)) * Math.Cos(2.0 * Math.PI * rand2);
            normrand[0] = normrand[0] * sigma + mu;

            normrand[1] = Math.Sqrt(-2.0 * Math.Log(rand1)) * Math.Sin(2.0 * Math.PI * rand2);
            normrand[1] = normrand[1] * sigma + mu;

            return normrand;
        }
    }
}
