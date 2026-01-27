using PSXPackagerGUI.Pages;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows;
using PSXPackagerGUI.Models.Resource;

namespace PSXPackagerGUI.Controls
{
    public partial class ImageEditorControl
    {

        private void InsertImageLayer(Layer? targetLayer)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(ResourceType);

            var result = openFileDialog.ShowDialog();

            if (result != true) return;

            using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);

            var image = ImageProcessing.GetBitmapImage(stream);

            int width = image.PixelWidth;
            int height = image.PixelHeight;
            int originalWidth = width;
            int originalHeight = height;

            double scale = Math.Min(
                (double)Composite.Width / image.PixelWidth,
                (double)Composite.Height / image.PixelHeight);

            if (scale < 1)
            {
                var resizeResult = MessageBox.Show(App.Current.MainWindow,
                    "The selected image is larger than the content area. Do you want to resize it to fit?",
                    "Load image", MessageBoxButton.YesNoCancel);

                if (resizeResult == MessageBoxResult.Yes)
                {
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }
            }

            var newLayer = new ImageLayer(image, "image", openFileDialog.FileName);

            newLayer.Width = width;
            newLayer.Height = height;
            newLayer.OriginalWidth = originalWidth;
            newLayer.OriginalHeight = originalHeight;

            Composite.PushState();

            if (targetLayer != null)
            {
                Composite.InsertLayerAfter(newLayer, targetLayer);
            }
            else
            {
                Composite.AddLayer(newLayer);
            }

            SelectedLayer = newLayer;

            UpdateSelection();
            Update();
        }

        private void InsertTextLayer(Layer? targetLayer)
        {
            var textEditorWindow = new TextEditorWindow();
            var model = textEditorWindow.Model;

            model.Text = "Sample Text";
            model.Color = Brushes.White;
            model.DropShadow = true;

            textEditorWindow.Owner = Application.Current.MainWindow;
            var result = textEditorWindow.ShowDialog();

            if (result is not true) return;

            var newLayer = new TextLayer("Text", model.Text, model.FontFamily, model.FontSize, model.Color, model.DropShadow, Composite.Width - 20, Composite.Height);

            Composite.PushState();

            if (targetLayer != null)
            {
                Composite.InsertLayerAfter(newLayer, targetLayer);
            }
            else
            {
                Composite.AddLayer(newLayer);
            }

            SelectedLayer = newLayer;

            UpdateSelection();
            Update();
        }

        private void ResetLayer(Layer layer)
        {
            Composite.PushState();
            
            layer.Reset();

            UpdateSelection();
            Update();
        }

        private void RemoveLayer(Layer layer)
        {
            Composite.PushState();
            Composite.Layers.Remove(layer);

            if (layer == SelectedLayer)
            {
                SelectedLayer = null;
            }
            
            UpdateSelection();
            Update();
        }

    }
}
