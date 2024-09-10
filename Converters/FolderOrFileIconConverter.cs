// FolderOrFileIconConverter.cs
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// Converts a boolean value indicating whether an item is a folder to the appropriate icon.
    /// </summary>
    public class FolderOrFileIconConverter : IValueConverter
    {
        #region Fields
        private static readonly BitmapImage FolderIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/folder.png"));
        private static readonly BitmapImage FileIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/file.png"));
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts a boolean value to the appropriate icon.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The appropriate icon based on the boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFolder)
            {
                return isFolder ? FolderIcon : FileIcon;
            }
            return null;
        }

        /// <summary>
        /// Converts an icon back to a boolean value.
        /// </summary>
        /// <remarks>
        /// This method is not implemented and will throw a NotImplementedException if called.
        /// </remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}