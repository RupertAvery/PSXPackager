using Popstation;
using Popstation.Pbp;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Pages;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PSXPackagerGUI.Controls
{

    /// <summary>
    /// Interaction logic for Resource.xaml
    /// </summary>
    public partial class ResourceControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string),
                typeof(ResourceControl));

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(ResourceType),
                typeof(ResourceControl));

        public static readonly RoutedEvent MoreEvent =
            EventManager.RegisterRoutedEvent(nameof(More), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly RoutedEvent RemoveEvent =
            EventManager.RegisterRoutedEvent(nameof(Remove), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register(nameof(Resource), typeof(ResourceModel),
                typeof(ResourceControl), new PropertyMetadata(null, OnResourceChanged));

        private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = ((ResourceControl)d);
            control.Resource = (ResourceModel)e.NewValue;
            control.InvalidateVisual();
            control.Resource.PropertyChanged += ResourceOnPropertyChanged;

            void ResourceOnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
            {
                if (propertyChangedEventArgs.PropertyName == nameof(ResourceModel.Icon))
                {
                    //control.InvalidateVisual();
                }
            }
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
            set { SetValue(ResourceProperty, value); InvalidateVisual(); }
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

        public ResourceControl()
        {
            InitializeComponent();
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
            set { _selectedLayer = value;
                if (Resource.Composite != null)
                {
                    Resource.Composite.SelectedLayer = _selectedLayer;
                }
                OnPropertyChanged();
            }
        }

        private double startX;
        private double startY;
        private Layer? _selectedLayer;
        private bool resizeMode;
        private bool dragStarted;

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

                if (slayer != null)
                {
                    var cornerX = slayer.OffsetX + slayer.Width;
                    var cornerY = slayer.OffsetY + slayer.Height;

                    if (pos.X >= cornerX - 2 && pos.X <= cornerX + 2 &&
                        pos.Y >= cornerY - 2 && pos.Y <= cornerY + 2)
                    {
                        resizeMode = true;
                    }
                }


                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    foreach (var layer in Resource.Composite.Layers.Reverse())
                    {
                        if (pos.X >= layer.OffsetX && pos.X <= layer.OffsetX + layer.Width &&
                            pos.Y >= layer.OffsetY && pos.Y <= layer.OffsetY + layer.Height)
                        {
                            SelectedLayer = layer;
                            Resource.RefreshIcon();
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
            else if(dragStarted)
            {
                SelectedLayer.OffsetX += deltaX;
                SelectedLayer.OffsetY += deltaY;
            }

            startX = pos.X;
            startY = pos.Y;

            if (resizeMode || dragStarted)
            {
                Resource.Composite.Render();
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
            Resource.RefreshIcon();
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
            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

            var result = openFileDialog.ShowDialog();
            if (result != true)
                return;

            using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
            var newLayer = new ImageLayer(ImageProcessing.GetBitmapImage(stream), "image", openFileDialog.FileName);

            Resource.Composite.AddLayer(newLayer);

            SelectedLayer = newLayer;
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
                Resource.RefreshIcon();
            }
        }

        private void InsertImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Resource.Composite != null && TryGetLayer(sender, out var layer))
            {
                var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
                openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

                var result = openFileDialog.ShowDialog();
                if (result != true)
                    return;

                using var stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                var newLayer = new ImageLayer(ImageProcessing.GetBitmapImage(stream), "image", openFileDialog.FileName);

                Resource.Composite.InsertLayerAfter(newLayer, layer);

                SelectedLayer = newLayer;
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
