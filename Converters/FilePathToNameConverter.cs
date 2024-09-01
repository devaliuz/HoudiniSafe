//*FilePathToNameConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that extracts the file name from a full file path.
    /// </summary>
    public class FilePathToNameConverter : IValueConverter
    {
        /// <summary>
        /// Converts a file path to just the file name.
        /// </summary>
        /// <param name="value">The full file path.</param>
        /// <param name="targetType">The type of the target property (should be a string).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>The file name extracted from the file path, or the original value if not a string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a string representing a file path
            if (value is string filePath)
            {
                // Extract and return the file name from the file path
                return Path.GetFileName(filePath);
            }

            // Return the original value if it is not a string
            return value;
        }

        /// <summary>
        /// ConvertBack is not implemented for this converter.
        /// </summary>
        /// <param name="value">The value to convert back (not used).</param>
        /// <param name="targetType">The type of the target property (not used).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion (not used).</param>
        /// <param name="culture">The culture to use in the conversion (not used).</param>
        /// <returns>Throws <see cref="NotImplementedException"/> as this conversion is not supported.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Throw NotImplementedException as this method is not implemented
            throw new NotImplementedException();
        }
    }
}
