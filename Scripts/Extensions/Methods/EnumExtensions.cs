
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class EnumExtensions
    {
        private static readonly Dictionary<Type, Dictionary<object, Dictionary<int, string>>> labelCache = new Dictionary<Type, Dictionary<object, Dictionary<int, string>>>();

        /// <summary> EnumのLabelAttributeで指定された名前を取得. </summary>
        public static string ToLabelName(this Enum @enum, int no = 0)
        {
            return LabelAttributeUtility.ToLabelName(@enum, @enum.GetType(), no);
        }

        public static bool HasFlag<T>(this T source, T destination) where T : struct, IComparable, IFormattable, IConvertible
        {
            return (Convert.ToUInt64(source) & Convert.ToUInt64(destination)) != 0;
        }

        /// <summary> Enum名で検索し値を返します. </summary>
        public static T FindByName<T>(string name, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(name)) { return defaultValue; }

            var type = typeof(T);

            var index = Enum.GetNames(type).IndexOf(x => x == name);
            
            return index != -1 ? Enum.GetValues(type).Cast<T>().ElementAtOrDefault(index) : defaultValue;
        }

        /// <summary> フラグ設定. </summary>
        public static T SetFlag<T>(this Enum value, T flag)
        {
            var underlyingType = Enum.GetUnderlyingType(value.GetType());

            dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
            dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);

            valueAsInt |= flagAsInt;

            return (T)valueAsInt;
        }

        /// <summary> フラグ解除. </summary>
        public static T RemoveFlag<T>(this Enum value, T flag)
        {
            var underlyingType = Enum.GetUnderlyingType(value.GetType());

            dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
            dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);

            valueAsInt &= ~flagAsInt;

            return (T)valueAsInt;
        }
    }
}
