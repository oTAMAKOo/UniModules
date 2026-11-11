
using System;

namespace Extensions
{
    public static class CommandLineUtility
    {
        public static T Get<T>(string label, T defaultValue = default)
        {
            var arguments = System.Environment.GetCommandLineArgs();

            var index = arguments.IndexOf(x => x == label);

            if (index == -1) { return defaultValue; }

            var valueIndex = index + 1;

            if (arguments.Length <= valueIndex) { return defaultValue; }

            var valueStr = arguments[valueIndex];

            var value = (T)Convert.ChangeType(valueStr, typeof(T));

            return value;
        }
    }
}