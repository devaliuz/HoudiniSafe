// FolderOrFileIconConverter.cs
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace HoudiniSafe.Converters
{
    /// <summary>
    /// A converter that returns an icon based on whether a path represents a folder or a file.
    /// </summary>
    public class FolderOrFileIconConverter : IValueConverter
    {
        #region Fields

        /// <summary>
        /// Scales an image to the specified width and height.
        /// </summary>
        /// <param name="imageSource">The source image to scale.</param>
        /// <param name="width">The desired width of the scaled image.</param>
        /// <param name="height">The desired height of the scaled image.</param>
        /// <returns>A new <see cref="ImageSource"/> object that is the scaled version of the input image.</returns>
        private static ImageSource ScaleImage(ImageSource imageSource, int width, int height)
        {
            var bitmap = new BitmapImage();

            // Create a memory stream to hold the encoded image
            using (var stream = new MemoryStream())
            {
                // Create a PNG encoder for the image
                var encoder = new PngBitmapEncoder();

                // Create a visual to render the image
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Draw the image within the specified width and height
                    var drawing = new ImageDrawing(imageSource, new Rect(new Size(width, height)));
                    context.DrawDrawing(drawing);
                }

                // Render the visual to a bitmap
                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);

                // Encode the bitmap to the memory stream
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
                stream.Position = 0;

                // Initialize the BitmapImage with the stream data
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze the bitmap for thread safety
            }

            return bitmap;
        }

        /// <summary>
        /// The icon used for folders.
        /// </summary>
        private static readonly ImageSource FolderIcon = ScaleImage((ImageSource)Application.Current.Resources["FolderIcon"], 20, 20);

        /// <summary>
        /// The icon used for files.
        /// </summary>
        private static readonly ImageSource FileIcon = ScaleImage((ImageSource)Application.Current.Resources["FileIcon"], 20, 20);

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts a boolean value to an icon, based on whether it represents a folder or a file.
        /// </summary>
        /// <param name="value">The value to convert, which should be a boolean indicating a folder (true) or file (false).</param>
        /// <param name="targetType">The type of the target property (should be <see cref="ImageSource"/>).</param>
        /// <param name="parameter">An optional parameter that can be used to influence the conversion.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>An <see cref="ImageSource"/> object representing either a folder or file icon.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean indicating whether it's a folder
            if (value is bool isFolder)
            {
                // Return the folder icon if true, otherwise the file icon
                return isFolder ? FolderIcon : FileIcon;
            }

            // Default to null if the input value is not a boolean
            return null;
        }

        /// <summary>
        /// Converts back from an icon to a boolean, which is not implemented in this converter.
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

        #endregion
    }
}
