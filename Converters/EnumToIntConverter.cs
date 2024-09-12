using System;
using System.Globalization;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that converts an <see cref="Enum"/> to an <see cref="int"/> and vice versa.
    /// </summary>
    public class EnumToIntConverter : IValueConverter
    {
        /// <summary>
        /// Converts an <see cref="Enum"/> value to an <see cref="int"/> value.
        /// </summary>
        /// <param name="value">The enum value to convert.</param>
        /// <param name="targetType">The type of the target property (should be <see cref="int"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>An <see cref="int"/> value representing the input enum value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is an Enum
            if (value is Enum enumValue)
            {
                // Convert the Enum to its integer representation
                return System.Convert.ToInt32(enumValue);
            }

            // Default to 0 if the value is not an Enum
            return 0;
        }

        /// <summary>
        /// Converts an <see cref="int"/> value back to an <see cref="Enum"/> value.
        /// </summary>
        /// <param name="value">The integer value to convert.</param>
        /// <param name="targetType">The type of the target property (should be an Enum type).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>An <see cref="Enum"/> value that corresponds to the input integer value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is an integer and the target type is an Enum
            if (value is int intValue && targetType.IsEnum)
            {
                // Convert the integer back to the corresponding Enum value
                return Enum.ToObject(targetType, intValue);
            }

            // Default to the first value of the Enum type if the conversion is not possible
            return Enum.GetValues(targetType).GetValue(0);
        }
    }
}
