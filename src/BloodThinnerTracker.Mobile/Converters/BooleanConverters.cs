using System.Globalization;

namespace BloodThinnerTracker.Mobile.Converters
{
    /// <summary>
    /// Inverts a boolean value (true -> false, false -> true).
    /// Useful for enabling/disabling buttons when loading.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }

    /// <summary>
    /// Returns true if the value is not null and not an empty string.
    /// Useful for conditionally showing error messages.
    /// </summary>
    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
