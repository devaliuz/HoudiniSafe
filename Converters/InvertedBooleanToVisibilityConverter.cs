// InvertedBooleanToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that inverts a <see cref="bool"/> value and converts it to a <see cref="Visibility"/> value.
    /// </summary>
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a <see cref="Visibility"/> value by inverting it.
        /// </summary>
        /// <param name="value">The value to convert, which should be a boolean.</param>
        /// <param name="targetType">The type of the target property (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>
        /// A <see cref="Visibility"/> value: 
        /// Returns <see cref="Visibility.Collapsed"/> if the input boolean is true, 
        /// and <see cref="Visibility.Visible"/> if the input boolean is false.
        /// Defaults to <see cref="Visibility.Visible"/> if the input is not a boolean.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean
            if (value is bool boolean)
            {
                // Return Visibility.Collapsed if true, otherwise Visibility.Visible
                return boolean ? Visibility.Collapsed : Visibility.Visible;
            }

            // Default to Visibility.Visible if the value is not a boolean
            return Visibility.Visible;
        }

        /// <summary>
        /// Converts a <see cref="Visibility"/> value back to a boolean, which is not implemented in this converter.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>Throws a <see cref="NotImplementedException"/> since this method is not implemented.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert back is not supported for this converter
            throw new NotImplementedException();
        }
    }
}
