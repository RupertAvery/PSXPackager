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
using System.Collections.ObjectModel;
using System.Runtime;
using System.Xml.Serialization;
using PSXPackagerGUI.Templates;
using ImageLayer = PSXPackagerGUI.Models.Resource.ImageLayer;
using Layer = PSXPackagerGUI.Models.Resource.Layer;
using TextLayer = PSXPackagerGUI.Models.Resource.TextLayer;
using Popstation;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;
using Resource = PSXPackagerGUI.Templates.Resource;
using System.Windows.Media.Media3D;

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

        public static readonly RoutedEvent UpdatedEvent =
            EventManager.RegisterRoutedEvent(nameof(Updated), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));


        public static readonly RoutedEvent LoadEvent =
            EventManager.RegisterRoutedEvent(nameof(Load), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));


        public static readonly RoutedEvent SaveEvent =
            EventManager.RegisterRoutedEvent(nameof(Save), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public event RoutedEventHandler Updated
        {
            add => AddHandler(UpdatedEvent, value);
            remove => RemoveHandler(UpdatedEvent, value);
        }

        public event RoutedEventHandler Load
        {
            add => AddHandler(LoadEvent, value);
            remove => RemoveHandler(LoadEvent, value);
        }

        public event RoutedEventHandler Save
        {
            add => AddHandler(SaveEvent, value);
            remove => RemoveHandler(SaveEvent, value);
        }

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


        public SettingsModel Settings => ServiceLocator.Settings;

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
            SizeChanged += (s, e) => UpdateSelection();
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
                    UpdateSelection();
                    Update();
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

                    if (pos.X >= cornerX - 6 && pos.X <= cornerX + 6 &&
                        pos.Y >= cornerY - 6 && pos.Y <= cornerY + 6)
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
                Update();
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
                    Update();
                }
            }
        }

        private void MoveDownLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Composite.MoveLayerDown(layer);
                Update();
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
            Update();
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
                Update();
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

                int width = image.PixelWidth;
                int height = image.PixelHeight;

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
                newLayer.OriginalWidth = width;
                newLayer.OriginalHeight = height;

                Composite.InsertLayerAfter(newLayer, layer);

                SelectedLayer = newLayer;
                UpdateSelection();
                Update();
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
                    Update();
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
                Update();
            }
        }

        private void ReplaceLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Update();
            }
        }

        private void MoveUpLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                Composite.MoveLayerUp(layer);
                Update();
            }
        }

        private void ResetLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                layer.Reset();
                UpdateSelection();
                Update();
            }

        }

        private bool TryGetLayer(object sender, out Layer? layer)
        {
            switch (sender)
            {
                case MenuItem { DataContext: Layer l }:
                    layer = l;
                    return true;
                case MenuItem { DataContext: ImageComposite model }:
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
                Update();
            }
        }

        private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            Composite.Render();
            Update();
        }

        private void EditText_OnClick(object sender, RoutedEventArgs e)
        {
            if (Composite != null && TryGetLayer(sender, out var layer))
            {
                EditText((TextLayer)layer);
                Update();
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
                UpdateSelection();
                Update();
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

        private void Update()
        {
            var newEventArgs = new RoutedEventArgs(UpdatedEvent, this);
            RaiseEvent(newEventArgs);
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();

            openFileDialog.InitialDirectory = Settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");

            openFileDialog.Filter = "Template files|*.xml|All files|*.*";

            var result = openFileDialog.ShowDialog();

            if (result is true)
            {
                var basePath = Path.GetDirectoryName(openFileDialog.FileName);

                Settings.LastTemplateDirectory = basePath;

                XmlSerializer serializer = new XmlSerializer(typeof(Resource));

                using (FileStream stream = File.OpenRead(openFileDialog.FileName))
                {
                    try
                    {
                        var xmlResource = (Resource)serializer.Deserialize(stream)!;
                        var resourceTemplate = xmlResource.ToResourceTemplate(basePath!);
                        if (resourceTemplate.ResourceType != ResourceType)
                        {
                            var confirmResult = MessageBox.Show(Application.Current.MainWindow,
                                $"The selected template does not match the resource type {ResourceType}. Are you sure you want to continue?",
                                "PSXPackager",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning
                            );

                            if (confirmResult == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                        Composite.Layers = new ObservableCollection<Layer>(resourceTemplate.Layers);
                        Update();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(Application.Current.MainWindow, $"Failed to load template:\n{exception.Message}", "PSXPackager",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SaveAsTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = Settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Template files|*.xml|All files|*.*";
            var result = saveFileDialog.ShowDialog();

            if (result is true)
            {

                var basePath = Path.GetDirectoryName(saveFileDialog.FileName);

                Settings.LastTemplateDirectory = basePath;

                var resourceTemplate = new ResourceTemplate
                {
                    ResourceType = ResourceType,
                    Width = Composite.Width,
                    Height = Composite.Height,
                    Layers = Composite.Layers.ToList()
                };

                var template = resourceTemplate.ToTemplateResource();

                foreach (var layer in template.Layers)
                {
                    if (layer is Templates.ImageLayer imageLayer)
                    {
                        if (Path.GetDirectoryName(imageLayer.SourceUri) == basePath)
                        {
                            imageLayer.SourceUri = Path.GetRelativePath(basePath!, imageLayer.SourceUri);
                        }
                    }
                }

                XmlSerializer serializer = new XmlSerializer(typeof(Resource));
                using (TextWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    serializer.Serialize(writer, template);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(SaveEvent, this);
            RaiseEvent(newEventArgs);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(LoadEvent, this);
            RaiseEvent(newEventArgs);
        }

        private void FitBounds_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer != null)
            {
                double scale = Math.Min(
                    (double)Composite.Width / SelectedLayer.Width,
                    (double)Composite.Height / SelectedLayer.Height);

                SelectedLayer.Width = SelectedLayer.OriginalWidth * scale;
                SelectedLayer.Height = SelectedLayer.OriginalHeight * scale;

                UpdateSelection();
                Update();
            }
        }

        private void FitWidth_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer != null)
            {
                double scale = (double)Composite.Width / SelectedLayer.Width;

                SelectedLayer.Width = SelectedLayer.OriginalWidth * scale;
                SelectedLayer.Height = SelectedLayer.OriginalHeight * scale;

                UpdateSelection();
                Update();
            }
        }

        private void FitHeight_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer != null)
            {
                double scale = (double)Composite.Height / SelectedLayer.Height;

                SelectedLayer.Width = SelectedLayer.OriginalWidth * scale;
                SelectedLayer.Height = SelectedLayer.OriginalHeight * scale;

                UpdateSelection();
                Update();
            }
        }
    }
}
