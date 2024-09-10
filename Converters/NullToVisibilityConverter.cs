//NullToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that converts a null value to <see cref="Visibility.Collapsed"/> and non-null values to <see cref="Visibility.Visible"/>.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a null value to <see cref="Visibility.Collapsed"/> and non-null values to <see cref="Visibility.Visible"/>.
        /// </summary>
        /// <param name="value">The value to convert. If the value is null, <see cref="Visibility.Collapsed"/> is returned; otherwise, <see cref="Visibility.Visible"/> is returned.</param>
        /// <param name="targetType">The type of the target property (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion (not used).</param>
        /// <param name="culture">The culture to use for the conversion (not used).</param>
        /// <returns><see cref="Visibility.Collapsed"/> if <paramref name="value"/> is null; otherwise, <see cref="Visibility.Visible"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Throws <see cref="NotImplementedException"/> as this converter does not support conversion back from <see cref="Visibility"/> to the original value.
        /// </summary>
        /// <param name="value">The value to convert back (not used).</param>
        /// <param name="targetType">The type of the target property (not used).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion (not used).</param>
        /// <param name="culture">The culture to use for the conversion (not used).</param>
        /// <returns>Throws <see cref="NotImplementedException"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
