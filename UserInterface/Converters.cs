using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Vcpkg
{
    public class StringAffixConverter : IValueConverter
    {
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Prefix + value as string + Suffix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class IsEmptyConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch(value)
            {
                case null:
                    return TrueValue;
                case ICollection collection:
                    if (collection.Count == 0)
                        return TrueValue;
                    goto default;
                case IEnumerable enumerable:
                    if (!enumerable.GetEnumerator().MoveNext())
                        return TrueValue;
                    goto default;
                default:
                    return FalseValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
