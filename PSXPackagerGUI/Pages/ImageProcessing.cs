using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Pages;

public static class ImageProcessing
{

    public static void SaveBitmapSource(BitmapSource bitmap, Stream stream, BitmapEncoder encoder)
    {
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }

    public static BitmapSource Resize(
        BitmapSource source,
        int maxWidth,
        int maxHeight)
    {
        double scale = Math.Min(
            (double)maxWidth / source.PixelWidth,
            (double)maxHeight / source.PixelHeight);

        if (scale >= 1.0)
            return source;

        int width = (int)(source.PixelWidth * scale);
        int height = (int)(source.PixelHeight * scale);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            RenderOptions.SetBitmapScalingMode(
                visual,
                BitmapScalingMode.HighQuality);

            dc.DrawImage(source, new Rect(0, 0, width, height));
        }

        var rtb = new RenderTargetBitmap(
            width,
            height,
            source.DpiX,
            source.DpiY,
            PixelFormats.Pbgra32);

        rtb.Render(visual);
        rtb.Freeze();
        return rtb;
    }

    public static BitmapSource ApplyAlphaMask(BitmapSource source, BitmapSource mask)
    {
        var visual = new DrawingVisual();

        using (DrawingContext dc = visual.RenderOpen())
        {
            dc.PushOpacityMask(new ImageBrush(mask));
            dc.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            dc.Pop();
        }

        var result = new RenderTargetBitmap(
            source.PixelWidth,
            source.PixelHeight,
            source.DpiX,
            source.DpiY,
            PixelFormats.Pbgra32);

        result.Render(visual);
        return result;
    }

    public static BitmapSource OverlayBitmaps(BitmapSource baseImage, BitmapSource overlayImage, int overlayX, int overlayY)
    {
        // Define the size of the final combined image
        int finalWidth = (int)baseImage.Width;
        int finalHeight = (int)baseImage.Height;

        // 1. Create a DrawingGroup
        var drawingGroup = new DrawingGroup();

        // 2. Add ImageDrawing objects for each image
        // Base image (drawn first, in the back)
        drawingGroup.Children.Add(new ImageDrawing(baseImage, new Rect(0, 0, finalWidth, finalHeight)));

        // Overlay image (drawn on top, at a specific position)
        drawingGroup.Children.Add(new ImageDrawing(overlayImage, new Rect(overlayX, overlayY, finalWidth, finalHeight)));

        // 3. Create a DrawingImage from the DrawingGroup
        var drawingImage = new DrawingImage(drawingGroup);

        // Set the dimensions for the drawing image to ensure proper rendering
        drawingImage.Freeze(); // Freeze for performance

        // 4. Render to a RenderTargetBitmap to get a new BitmapSource
        var renderTargetBitmap = new RenderTargetBitmap(
            finalWidth,
            finalHeight,
            96,
            96,
            PixelFormats.Pbgra32); // Use a format that supports transparency

        // Create a Visual to render the DrawingImage onto
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(drawingImage, new Rect(0, 0, finalWidth, finalHeight));
        }

        renderTargetBitmap.Render(visual);
        renderTargetBitmap.Freeze(); // Freeze for performance

        return renderTargetBitmap;
    }

}