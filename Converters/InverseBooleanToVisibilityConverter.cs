//InverseBooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that inversely maps a boolean value or integer to a <see cref="Visibility"/> value.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value or integer to <see cref="Visibility"/> by inverting the boolean logic.
        /// </summary>
        /// <param name="value">The value to convert, typically a boolean or integer.</param>
        /// <param name="targetType">The type of the target property (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion (not used).</param>
        /// <param name="culture">The culture to use in the conversion (not used).</param>
        /// <returns><see cref="Visibility.Visible"/> if the value is false or zero, otherwise <see cref="Visibility.Collapsed"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean
            if (value is bool boolValue)
            {
                // Return Visibility.Visible if boolean value is false, otherwise Visibility.Collapsed
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            // Check if the value is an integer
            else if (value is int intValue)
            {
                // Return Visibility.Visible if integer value is zero, otherwise Visibility.Collapsed
                return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Default to Visibility.Visible if the value is neither boolean nor integer
            return Visibility.Visible;
        }

        /// <summary>
        /// Converts back from <see cref="Visibility"/> to boolean.
        /// </summary>
        /// <param name="value">The <see cref="Visibility"/> value to convert back from.</param>
        /// <param name="targetType">The type of the target property (not used).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion (not used).</param>
        /// <param name="culture">The culture to use in the conversion (not used).</param>
        /// <returns>Returns true if <see cref="Visibility"/> is not <see cref="Visibility.Visible"/>, otherwise false.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is of type Visibility
            if (value is Visibility visibility)
            {
                // Return true if visibility is not Visible, otherwise false
                return visibility != Visibility.Visible;
            }

            // Default to true if the value is not of type Visibility
            return true;
        }
    }
}
