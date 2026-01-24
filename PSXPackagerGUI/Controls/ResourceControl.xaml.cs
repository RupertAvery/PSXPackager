using Popstation;
using Popstation.Pbp;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Pages;
using PSXPackagerGUI.Templates;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ImageLayer = PSXPackagerGUI.Models.Resource.ImageLayer;
using Layer = PSXPackagerGUI.Models.Resource.Layer;
using TextLayer = PSXPackagerGUI.Models.Resource.TextLayer;

namespace PSXPackagerGUI.Controls
{
    /// <summary>
    /// Interaction logic for Resource.xaml
    /// </summary>
    public partial class ResourceControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text),
                typeof(string),
                typeof(ResourceControl));

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type),
                typeof(ResourceType),
                typeof(ResourceControl));

        public static readonly RoutedEvent MoreEvent =
            EventManager.RegisterRoutedEvent(nameof(More),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly RoutedEvent RemoveEvent =
            EventManager.RegisterRoutedEvent(nameof(Remove),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ResourceControl));


        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register(nameof(Resource),
                typeof(ResourceModel),
                typeof(ResourceControl),
                new PropertyMetadata(null, OnResourceChanged));

        private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ResourceControl)d;

            // Optional: access old/new values
            var oldValue = (ResourceModel)e.OldValue;
            var newValue = (ResourceModel)e.NewValue;

            //var control = ((ResourceControl)d);

            //control.Resource = (ResourceModel)e.NewValue;
            //control.InvalidateVisual();
            //control.Resource.PropertyChanged += ResourceOnPropertyChanged;

            control.UpdateSelection();

            //void ResourceOnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
            //{
            //    if (propertyChangedEventArgs.PropertyName == nameof(ResourceModel.Icon))
            //    {
            //        //control.InvalidateVisual();
            //    }
            //}
        }

        public ResourceType Type
        {
            get => (ResourceType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public event RoutedEventHandler More
        {
            add => AddHandler(MoreEvent, value);
            remove => RemoveHandler(MoreEvent, value);
        }

        public event RoutedEventHandler Remove
        {
            add => AddHandler(RemoveEvent, value);
            remove => RemoveHandler(RemoveEvent, value);
        }

        public ResourceModel Resource
        {
            get => (ResourceModel)GetValue(ResourceProperty);
            set
            {
                SetValue(ResourceProperty, value);
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                SetValue(TextProperty, value);
                OnPropertyChanged();
            }
        }

        public Selection? Selection
        {
            get => _selection;
            set
            {
                _selection = value;
                OnPropertyChanged();
            }
        }

        public ResourceControl()
        {
            InitializeComponent();
            Selection = new Selection();
            SizeChanged += (s, e) => UpdateSelection();
        }

        private void More_OnClick(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(MoreEvent, this);
            RaiseEvent(newEventArgs);
        }

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(RemoveEvent, this);
            RaiseEvent(newEventArgs);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

        private double startX;
        private double startY;
        private Layer? _selectedLayer;
        private bool resizeMode;
        private bool dragStarted;
        private Selection? _selection;

        private void UpdateSelection()
        {
            if (Resource?.Composite == null)
            {
                return;
            }

            if (SelectedLayer == null)
            {
                Selection.Visibility = Visibility.Hidden;
                return;
            }

            Selection.Visibility = Visibility.Visible;
            var offsetX = (Grid.ActualWidth - Resource.Composite.Width) / 2;
            var offsetY = (Grid.ActualHeight - Resource.Composite.Height) / 2;

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
                    Resource.RefreshIcon();
                }
            }

            if (Resource.Composite?.Layers.Count > 0)
            {
                var image = (UIElement)sender;
                image.CaptureMouse();

                var pos = e.GetPosition(image);
                startX = pos.X;
                startY = pos.Y;

                resizeMode = false;
                dragStarted = true;

                var slayer = SelectedLayer;

                var offsetX = (Grid.ActualWidth - Resource.Composite.Width) / 2;
                var offsetY = (Grid.ActualHeight - Resource.Composite.Height) / 2;


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
                    foreach (var layer in Resource.Composite.Layers.Reverse())
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
                Resource.RefreshIcon();
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
                    Resource.RefreshIcon();
                }
            }
        }

        private void MoveDownLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                Resource.Composite.MoveLayerDown(layer);
                Resource.RefreshIcon();
            }
        }

        private void AppendImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

            var result = openFileDialog.ShowDialog();
            if (result != true)
                return;

            using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
            var newLayer = new ImageLayer(ImageProcessing.GetBitmapImage(stream), "image", openFileDialog.FileName);

            Resource.Composite.AddLayer(newLayer);

            SelectedLayer = newLayer;
            UpdateSelection();
            Resource.RefreshIcon();
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
                var newLayer = new TextLayer("Text", model.Text, model.FontFamily, model.FontSize, model.Color, model.DropShadow, Resource.Composite.Width - 20, Resource.Composite.Height);

                Resource.Composite.AddLayer(newLayer);

                SelectedLayer = newLayer;
                UpdateSelection();
                Resource.RefreshIcon();
            }
        }

        private void InsertImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

                var result = openFileDialog.ShowDialog();
                if (result != true)
                    return;

                using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);

                var image = ImageProcessing.GetBitmapImage(stream);

                double scale = Math.Min(
                    (double)Resource.Composite.Width / image.PixelWidth,
                    (double)Resource.Composite.Height / image.PixelHeight);

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

                var newLayer = new Models.Resource.ImageLayer(image, "image", openFileDialog.FileName);

                newLayer.Width = width;
                newLayer.Height = height;
                newLayer.OriginalWidth = width;
                newLayer.OriginalHeight = height;

                Resource.Composite.InsertLayerAfter(newLayer, layer);

                SelectedLayer = newLayer;
                UpdateSelection();
                Resource.RefreshIcon();
            }
        }

        private void InsertTextLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
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
                    var newLayer = new TextLayer("Text", model.Text, model.FontFamily, model.FontSize, model.Color, model.DropShadow, Resource.Composite.Width - 20, Resource.Composite.Height);

                    Resource.Composite.InsertLayerAfter(newLayer, layer);

                    SelectedLayer = newLayer;
                    UpdateSelection();
                    Resource.RefreshIcon();
                }
            }
        }

        private void RemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                Resource.Composite.Layers.Remove(layer);
                if (layer == SelectedLayer)
                {
                    SelectedLayer = null;
                }
                UpdateSelection();
                Resource.RefreshIcon();
            }
        }

        private void ReplaceLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                Resource.RefreshIcon();
            }
        }

        private void MoveUpLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                Resource.Composite.MoveLayerUp(layer);
                Resource.RefreshIcon();
            }
        }

        private void ResetLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                layer.Reset();
                UpdateSelection();
                Resource.RefreshIcon();
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
                        SelectedLayer = Resource.Composite.Layers.LastOrDefault();
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
                Resource.Composite.Render();
                Resource.RefreshIcon();
            }
        }

        private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Resource.Composite.Render();
            Resource.RefreshIcon();
        }

        private void EditText_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                EditText((TextLayer)layer);
                Resource.RefreshIcon();
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
                Resource.RefreshIcon();
                e.Handled = true;
            }
        }

        private void Border_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).Focus();
        }
    }
}
