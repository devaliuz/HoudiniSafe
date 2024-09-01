//BooleanToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that converts a <see cref="bool"/> or an <see cref="int"/> to a <see cref="Visibility"/> value.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean or integer value to a <see cref="Visibility"/> value.
        /// </summary>
        /// <param name="value">The value to convert, which can be a boolean or integer.</param>
        /// <param name="targetType">The type of the target property (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A <see cref="Visibility"/> value based on the input value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean
            if (value is bool boolValue)
            {
                // Return Visibility.Visible if the boolean is true, otherwise return Visibility.Collapsed
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            // Check if the value is an integer
            else if (value is int intValue)
            {
                // Return Visibility.Visible if the integer is greater than 0, otherwise return Visibility.Collapsed
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // For any other types, default to Visibility.Collapsed
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a <see cref="Visibility"/> value back to a boolean value.
        /// </summary>
        /// <param name="value">The <see cref="Visibility"/> value to convert.</param>
        /// <param name="targetType">The type of the target property (should be boolean).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>A boolean value representing the visibility.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a Visibility enum
            if (value is Visibility visibility)
            {
                // Return true if the visibility is Visible, otherwise return false
                return visibility == Visibility.Visible;
            }

            // Default to false if the value is not a Visibility enum
            return false;
        }
    }
}
