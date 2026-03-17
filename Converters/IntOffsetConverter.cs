using System.Globalization;
using System.Windows.Data;

namespace MiniIDEv04.Converters
{
    /// <summary>
    /// Converts between a 1-based int (Phase 1-4, Priority 1-3) and
    /// a 0-based SelectedIndex by applying an integer offset.
    /// ConverterParameter="-1" → value 1 becomes index 0, value 2 becomes index 1, etc.
    /// </summary>
    public class IntOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (value is int v && int.TryParse(parameter?.ToString(), out int offset))
                return v + offset;
            return 0;
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            if (value is int v && int.TryParse(parameter?.ToString(), out int offset))
                return v - offset;
            return 1;
        }
    }
}
