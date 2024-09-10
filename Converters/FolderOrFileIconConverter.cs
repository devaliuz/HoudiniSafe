// FolderOrFileIconConverter.cs
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace HoudiniSafe.Converters
{
    public class FolderOrFileIconConverter : IValueConverter
    {
        #region Fields

        private static ImageSource ScaleImage(ImageSource imageSource, int width, int height)
        {
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var drawing = new ImageDrawing(imageSource, new Rect(new Size(width, height)));
                    context.DrawDrawing(drawing);
                }

                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(stream);
                stream.Position = 0;

                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            return bitmap;
        }

        private static readonly ImageSource FolderIcon = ScaleImage((ImageSource)Application.Current.Resources["FolderIcon"], 20, 20);
        private static readonly ImageSource FileIcon = ScaleImage((ImageSource)Application.Current.Resources["FileIcon"], 20, 20);

        #endregion

        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFolder)
            {
                return isFolder ? FolderIcon : FileIcon;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
