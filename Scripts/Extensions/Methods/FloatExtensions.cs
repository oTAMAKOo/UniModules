
namespace Extensions
{
    public static class FloatExtensions
    {
        public static bool ValueInRange(this float value, float min, float max)
        {
            return (value >= min) && (value <= max);
        }
    }
}
