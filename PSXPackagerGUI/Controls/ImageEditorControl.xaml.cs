using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Pages;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Popstation.Pbp;
using System.Runtime.CompilerServices;

namespace PSXPackagerGUI.Controls
{
    /// <summary>
    /// Interaction logic for ImageEditorControl.xaml
    /// </summary>
    public partial class ImageEditorControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty CompositeProperty =
            DependencyProperty.Register(nameof(Composite),
                typeof(ImageComposite),
                typeof(ImageEditorControl),
                new PropertyMetadata(null, OnPropertyChangedCallback));

        private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageEditorControl)d;
            control.SelectedLayer = null;
            control.UpdateSelection();
        }

        public static readonly DependencyProperty ResourceTypeProperty =
            DependencyProperty.Register(nameof(ResourceType),
                typeof(ResourceType),
                typeof(ImageEditorControl),
                new PropertyMetadata(ResourceType.BOOT));

        public ImageComposite Composite { 
            get => (ImageComposite)GetValue(CompositeProperty);
            set => SetValue(CompositeProperty, value);
        }

        public ResourceType ResourceType
        {
            get => (ResourceType)GetValue(ResourceTypeProperty);
            set => SetValue(ResourceTypeProperty, value);
        }

        private double startX;
        private double startY;
        private Layer? _selectedLayer;
        private bool resizeMode;
        private bool dragStarted;
        private Selection? _selection;

    
        public Selection? Selection
        {
            get => _selection;
            set
            {
                _selection = value;
                OnPropertyChanged();
            }
        }

        public ImageEditorControl()
        {
            InitializeComponent();
            Selection = new Selection();
        }


        public Layer? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                UpdateSelection();
                OnPropertyChanged();
            }
        }

        private void UpdateSelection()
        {
            if (Composite == null)
            {
                return;
            }

            if (SelectedLayer == null)
            {
                Selection.Visibility = Visibility.Hidden;
                return;
            }

            Selection.Visibility = Visibility.Visible;
            var offsetX = (Grid.ActualWidth - Composite.Width) / 2;
            var offsetY = (Grid.ActualHeight - Composite.Height) / 2;

            var left = offsetX + SelectedLayer.OffsetX;
            var top = offsetY + SelectedLayer.OffsetY;
            var width = SelectedLayer.Width;
            var height = SelectedLayer.Height;

            Selection.C1.X = left - 2;
            Selection.C1.Y = top - 2;

            Selection.C2.X = left + width - 2;
            Selection.C2.Y = top - 2;

            Selection.C3.X = left + width - 2;
            Selection.C3.Y = top + height - 2;

            Selection.C4.X = left - 2;
            Selection.C4.Y = top + height - 2;

            Selection.E1.X = left;
            Selection.E1.Y = top;

            Selection.E2.X = left + width;
            Selection.E2.Y = top;

            Selection.E3.X = left + width;
            Selection.E3.Y = top + height;

            Selection.E4.X = left;
            Selection.E4.Y = top + height;

        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (e.ClickCount == 2)
            {
                if (SelectedLayer is TextLayer textLayer)
                {
                    EditText(textLayer);
                    //Resource.RefreshIcon();
                }
            }

            if (Composite?.Layers.Count > 0)
            {
                var image = (UIElement)sender;
                image.CaptureMouse();

                var pos = e.GetPosition(image);
                startX = pos.X;
                startY = pos.Y;

                resizeMode = false;
                dragStarted = true;

                var slayer = SelectedLayer;

                var offsetX = (Grid.ActualWidth - Composite.Width) / 2;
                var offsetY = (Grid.ActualHeight - Composite.Height) / 2;


                if (slayer != null)
                {

                    var cornerX = offsetX + slayer.OffsetX + slayer.Width;
                    var cornerY = offsetY + slayer.OffsetY + slayer.Height;

                    if (pos.X >= cornerX - 4 && pos.X <= cornerX + 4 &&
                        pos.Y >= cornerY - 4 && pos.Y <= cornerY + 4)
                    {
                        resizeMode = true;
                    }
                }


                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    foreach (var layer in Composite.Layers.Reverse())
                    {
                        if (pos.X >= offsetX + layer.OffsetX && pos.X <= offsetX + layer.OffsetX + layer.Width &&
                            pos.Y >= offsetY + layer.OffsetY && pos.Y <= offsetY + layer.OffsetY + layer.Height)
                        {
                            SelectedLayer = layer;
                            break;
                        }
                    }
                }
            }
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || SelectedLayer == null)
                return;

            var image = (UIElement)sender;
            var pos = e.GetPosition(image);

            var deltaX = pos.X - startX;
            var deltaY = pos.Y - startY;

            if (resizeMode)
            {
                SelectedLayer.Width += deltaX;
                SelectedLayer.Height += deltaY;
                SelectedLayer.Width = Math.Max(SelectedLayer.Width, 4);
                SelectedLayer.Height = Math.Max(SelectedLayer.Height, 4);
            }
            else if (dragStarted)
            {
                SelectedLayer.OffsetX += deltaX;
                SelectedLayer.OffsetY += deltaY;
            }

            startX = pos.X;
            startY = pos.Y;

            if (resizeMode || dragStarted)
            {
                UpdateSelection();
                //Resource.RefreshIcon();
            }
        }

        private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var image = (UIElement)sender;
            image.ReleaseMouseCapture();
            resizeMode = false;
            dragStarted = false;
        }

        private void Layer_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var layer = (sender as FrameworkElement)?.DataContext as Layer;
            SelectedLayer = layer;
            if (e.ClickCount == 2)
            {
                if (SelectedLayer is TextLayer textLayer)
                {
                    EditText(textLayer);
                    //Resource.RefreshIcon();
                }
            }
        }

        private void MoveDownLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Composite.MoveLayerDown(layer);
                //Resource.RefreshIcon();
            }
        }

        private void AppendImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(ResourceType);

            var result = openFileDialog.ShowDialog();
            if (result != true)
                return;

            using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
            var newLayer = new ImageLayer(ImageProcessing.GetBitmapImage(stream), "image", openFileDialog.FileName);

            Composite.AddLayer(newLayer);

            SelectedLayer = newLayer;
            UpdateSelection();
            //Resource.RefreshIcon();
        }

        private void AppendTextLayer_OnClick(object sender, RoutedEventArgs e)
        {
            var textEditorWindow = new TextEditorWindow();
            var model = textEditorWindow.Model;

            model.Text = "Sample Text";
            model.Color = Brushes.White;
            model.DropShadow = true;

            textEditorWindow.Owner = Application.Current.MainWindow;
            var result = textEditorWindow.ShowDialog();

            if (result is true)
            {
                var newLayer = new TextLayer("Text", model.Text, model.FontFamily, model.FontSize, model.Color, model.DropShadow, Composite.Width - 20, Composite.Height);

                Composite.AddLayer(newLayer);

                SelectedLayer = newLayer;
                UpdateSelection();
                //Resource.RefreshIcon();
            }
        }

        private void InsertImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = ImageProcessing.GetFilterFromType(ResourceType);

                var result = openFileDialog.ShowDialog();
                if (result != true)
                    return;

                using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);

                var image = ImageProcessing.GetBitmapImage(stream);

                double scale = Math.Min(
                    (double)Composite.Width / image.PixelWidth,
                    (double)Composite.Height / image.PixelHeight);

                int width = image.PixelWidth;
                int height = image.PixelHeight;

                if (scale >= 1)
                {
                    var resizeResult = MessageBox.Show(App.Current.MainWindow,
                        "The selected image is larger than the content area. Do you want to resize it to fit?",
                        "Load image", MessageBoxButton.YesNoCancel);

                    if (resizeResult == MessageBoxResult.Yes)
                    {
                        width = (int)(width * scale);
                        height = (int)(width * scale);

                    }
                }

                var newLayer = new ImageLayer(image, "image", openFileDialog.FileName);

                newLayer.Width = width;
                newLayer.Height = height;
                newLayer.OriginalWidth = width;
                newLayer.OriginalHeight = height;

                Composite.InsertLayerAfter(newLayer, layer);

                SelectedLayer = newLayer;
                UpdateSelection();
                //Resource.RefreshIcon();
            }
        }

        private void InsertTextLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                var textEditorWindow = new TextEditorWindow();
                var model = textEditorWindow.Model;

                model.Text = "Sample Text";
                model.Color = Brushes.White;
                model.DropShadow = true;

                textEditorWindow.Owner = Application.Current.MainWindow;
                var result = textEditorWindow.ShowDialog();

                if (result is true)
                {
                    var newLayer = new TextLayer("Text", model.Text, model.FontFamily, model.FontSize, model.Color, model.DropShadow, Composite.Width - 20, Composite.Height);

                    Composite.InsertLayerAfter(newLayer, layer);

                    SelectedLayer = newLayer;
                    UpdateSelection();
                    //Resource.RefreshIcon();
                }
            }
        }

        private void RemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Composite.Layers.Remove(layer);
                if (layer == SelectedLayer)
                {
                    SelectedLayer = null;
                }
                UpdateSelection();
                //Resource.RefreshIcon();
            }
        }

        private void ReplaceLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                //Resource.RefreshIcon();
            }
        }

        private void MoveUpLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Composite.MoveLayerUp(layer);
                //Resource.RefreshIcon();
            }
        }

        private void ResetLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                layer.Reset();
                UpdateSelection();
                //Resource.RefreshIcon();
            }

        }

        private bool TryGetLayer(object sender, out Layer? layer)
        {
            switch (sender)
            {
                case MenuItem { DataContext: Layer l }:
                    layer = l;
                    return true;
                case MenuItem { DataContext: ResourceModel model }:
                    if (SelectedLayer == null)
                    {
                        SelectedLayer = Composite.Layers.LastOrDefault();
                    }
                    layer = SelectedLayer;
                    return true;
            }
            layer = null;
            return false;
        }

        private void Redraw(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Composite.Render();
                //Resource.RefreshIcon();
            }
        }

        private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Composite.Render();
            //Resource.RefreshIcon();
        }

        private void EditText_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                EditText((TextLayer)layer);
                //Resource.RefreshIcon();
            }
        }

        private void EditText(TextLayer textLayer)
        {
            var textEditorWindow = new TextEditorWindow();
            textEditorWindow.Owner = Application.Current.MainWindow;

            var model = textEditorWindow.Model;

            model.Text = textLayer.TextContent;
            model.FontFamily = textLayer.FontFamily;
            model.FontSize = textLayer.FontSize;
            model.Color = textLayer.Color;
            model.DropShadow = textLayer.DropShadow;

            var result = textEditorWindow.ShowDialog();

            if (result is true)
            {
                textLayer.TextContent = model.Text;
                textLayer.FontFamily = model.FontFamily;
                textLayer.FontSize = model.FontSize;
                textLayer.Color = model.Color;
                textLayer.DropShadow = model.DropShadow;
                textLayer.RecalculateExtents();
            }
        }

        private void ResourceControl_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right && SelectedLayer != null)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        SelectedLayer.OffsetX--;
                        break;
                    case Key.Right:
                        SelectedLayer.OffsetX++;
                        break;
                    case Key.Up:
                        SelectedLayer.OffsetY--;
                        break;
                    case Key.Down:
                        SelectedLayer.OffsetY++;
                        break;
                }
                //Resource.RefreshIcon();
                e.Handled = true;
            }
        }

        private void Border_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).Focus();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
