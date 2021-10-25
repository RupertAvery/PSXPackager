using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Controls
{
    public class AutoDisableImage : Image
    {
        public AutoDisableImage()
        {

        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property.Name == nameof(IsEnabled))
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            BitmapSource bitmapSource = Source as BitmapSource;
            if (bitmapSource == null)
            {
                return;
            }

            if (!IsEnabled)
            {
                // Disable gray
                OpacityMask = new ImageBrush(bitmapSource);
                bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Gray32Float, null, 1);

            }

            dc.DrawImage(bitmapSource, new Rect(new Point(), RenderSize));
        }
    }
}