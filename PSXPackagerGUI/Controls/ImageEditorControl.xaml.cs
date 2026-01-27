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
using System.Xml.Serialization;
using ImageLayer = PSXPackagerGUI.Models.Resource.ImageLayer;
using Layer = PSXPackagerGUI.Models.Resource.Layer;
using TextLayer = PSXPackagerGUI.Models.Resource.TextLayer;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;
using Resource = PSXPackagerGUI.Templates.Resource;

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
            control.Update();
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

        public static readonly RoutedEvent HoverEvent =
            EventManager.RegisterRoutedEvent(nameof(Hover), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly RoutedEvent SelectEvent =
            EventManager.RegisterRoutedEvent(nameof(Select), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
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

        public event RoutedEventHandler Hover
        {
            add => AddHandler(HoverEvent, value);
            remove => RemoveHandler(HoverEvent, value);
        }

        public event RoutedEventHandler Select
        {
            add => AddHandler(SelectEvent, value);
            remove => RemoveHandler(SelectEvent, value);
        }
        
        public ImageComposite Composite
        {
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
        private double startOffsetX;
        private double startOffsetY;
        private double startWidth;
        private double startHeight;
        private Layer? _selectedLayer;

        private bool resizeMode;
        private bool dragStarted;
        private Selection _selection;


        public SettingsModel Settings => ServiceLocator.Settings;

        public Selection Selection
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
            Keyboard.Focus((UIElement)sender);

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Composite.SaveState();

            if (e.ClickCount == 2)
            {
                if (SelectedLayer is TextLayer textLayer)
                {
                    if (EditText(textLayer))
                    {
                        if (SelectedLayer is { IsDirty: true })
                        {
                            Composite.CommitState();
                            SelectedLayer.SetPristine();
                        }

                        UpdateSelection();
                        Update();
                    }
                }
            }

            if (Composite.Layers.Count > 0)
            {
                var image = (UIElement)sender;

                var pos = e.GetPosition(image);
                startX = pos.X;
                startY = pos.Y;

                resizeMode = false;
                dragStarted = true;

                var offsetX = (Grid.ActualWidth - Composite.Width) / 2;
                var offsetY = (Grid.ActualHeight - Composite.Height) / 2;


                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    foreach (var layer in Composite.Layers.Reverse())
                    {
                        if (pos.X >= offsetX + layer.OffsetX && pos.X <= offsetX + layer.OffsetX + layer.Width &&
                            pos.Y >= offsetY + layer.OffsetY && pos.Y <= offsetY + layer.OffsetY + layer.Height)
                        {
                            SelectedLayer = layer;
                            var newEventArgs = new SelectLayerEventArgs(SelectEvent, this, layer);
                            RaiseEvent(newEventArgs);
                            break;
                        }
                    }
                }

                var slayer = SelectedLayer;
                
                if (slayer != null)
                {
                    startOffsetX = slayer.OffsetX;
                    startOffsetY = slayer.OffsetY;

                    var cornerX = offsetX + slayer.OffsetX + slayer.Width;
                    var cornerY = offsetY + slayer.OffsetY + slayer.Height;

                    if (pos.X >= cornerX - 6 && pos.X <= cornerX + 6 &&
                        pos.Y >= cornerY - 6 && pos.Y <= cornerY + 6)
                    {
                        startWidth = slayer.Width;
                        startHeight = slayer.Height;
                        resizeMode = true;
                    }
                }

                image.CaptureMouse();
            }
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            var element = (UIElement)sender;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(element);

                var offsetX = (Grid.ActualWidth - Composite.Width) / 2;
                var offsetY = (Grid.ActualHeight - Composite.Height) / 2;

                foreach (var layer in Composite.Layers.Reverse())
                {
                    if (pos.X >= offsetX + layer.OffsetX && pos.X <= offsetX + layer.OffsetX + layer.Width &&
                        pos.Y >= offsetY + layer.OffsetY && pos.Y <= offsetY + layer.OffsetY + layer.Height)
                    {
                        var newEventArgs = new HoverEventArgs(HoverEvent, this, layer, SelectedLayer == layer);
                        RaiseEvent(newEventArgs);
                        return;
                    }
                }

                var noneEventArgs = new HoverEventArgs(HoverEvent, this, null, false);
                RaiseEvent(noneEventArgs);
            }
            else
            {

                if (SelectedLayer == null)
                {
                    return;
                }

                var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                var pos = e.GetPosition(element);

                var deltaX = pos.X - startX;
                var deltaY = pos.Y - startY;

                if (Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1)
                {
                    if (resizeMode)
                    {
                        SelectedLayer.Width = Math.Max(startWidth + deltaX, 4);
                        SelectedLayer.Height = Math.Max(startHeight + deltaY, 4);
                    }
                    else if (dragStarted)
                    {
                        SelectedLayer.OffsetX = startOffsetX + deltaX;
                        SelectedLayer.OffsetY = startOffsetY + deltaY;
                    }

                    //startX = pos.X;
                    //startY = pos.Y;
                    if (resizeMode || dragStarted)
                    {
                        UpdateSelection();
                        Update();
                    }
                }
    
            }

        }

        private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (SelectedLayer is { IsDirty: true })
            {
                Composite.CommitState();
                SelectedLayer.SetPristine();
            }

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
                    Composite.SaveState();
                    if (EditText(textLayer))
                    {
                        Composite.CommitState();
                        Update();
                    }
                }
            }
        }

        private void InsertImageLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                InsertImageLayer(layer);
            }
        }

        private void InsertTextLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                InsertTextLayer(layer);
            }
        }

        private void RemoveLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                RemoveLayer(layer);
            }
        }

        private void ResetLayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                ResetLayer(layer);
            }
        }

        private void MoveLayerUp_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                Composite.PushState();
                Composite.MoveLayerUp(layer);
                Update();
            }
        }

        private void MoveLayerDown_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                Composite.PushState();
                Composite.MoveLayerDown(layer);
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
                if (SelectedLayer is { IsDirty: true })
                {
                    Composite.CommitState();
                    SelectedLayer.SetPristine();
                }

                UpdateSelection();
                Update();
            }
        }


        private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer is { IsDirty: true })
            {
                Composite.CommitState();
                SelectedLayer.SetPristine();
            }

            UpdateSelection();
            Update();
        }

        private void EditText_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryGetLayer(sender, out var layer))
            {
                Composite.SaveState();
                if (EditText((TextLayer)layer))
                {
                    Composite.CommitState();
                    Update();
                }
            }
        }

        private bool EditText(TextLayer textLayer)
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

                return true;
            }

            return false;
        }

        private void Update()
        {
            Composite?.Render();

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
                Composite.PushState();
                double xscale = (double)Composite.Width / SelectedLayer.Width;
                double yscale = (double)Composite.Height / SelectedLayer.Height;

                SelectedLayer.OffsetX = 0;
                SelectedLayer.OffsetY = 0;

                SelectedLayer.Width = SelectedLayer.Width * xscale;
                SelectedLayer.Height = SelectedLayer.Height * yscale;

                UpdateSelection();
                Update();
            }
        }

        private void FitWidth_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer != null)
            {
                Composite.PushState();
                double scale = (double)Composite.Width / SelectedLayer.Width;

                SelectedLayer.OffsetX = 0;
                SelectedLayer.OffsetY = 0;

                SelectedLayer.Width = SelectedLayer.Width * scale;
                SelectedLayer.Height = SelectedLayer.Height * scale;

                UpdateSelection();
                Update();
            }
        }

        private void FitHeight_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLayer != null)
            {
                Composite.PushState();
                double scale = (double)Composite.Height / SelectedLayer.Height;

                SelectedLayer.OffsetX = 0;
                SelectedLayer.OffsetY = 0;

                SelectedLayer.Width = SelectedLayer.Width * scale;
                SelectedLayer.Height = SelectedLayer.Height * scale;

                UpdateSelection();
                Update();
            }
        }

        private void ClearLayers_OnClick(object sender, RoutedEventArgs e)
        {
            Composite.PushState();
            Composite.Layers.Clear();
            SelectedLayer = null;
            UpdateSelection();
            Update();
        }

        private void Grid_OnKeyDown(object sender, KeyEventArgs e)
        {
            var crtlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            switch (e.Key)
            {
                case Key.Z when crtlPressed:
                    UndoState();
                    e.Handled = true;
                    break;
                case Key.Y when crtlPressed:
                    RedoState();
                    e.Handled = true;
                    break;
                case Key.Up or Key.Down or Key.Left or Key.Right when SelectedLayer != null:
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
                    break;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }

}
