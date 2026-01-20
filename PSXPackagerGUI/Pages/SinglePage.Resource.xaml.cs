using Microsoft.Win32;
using Popstation.Pbp;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Controls;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Templates;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using ImageLayer = PSXPackagerGUI.Models.Resource.ImageLayer;
using Layer = PSXPackagerGUI.Models.Resource.Layer;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Single.xaml
    /// </summary>
    public partial class SinglePage : Page, INotifier
    {

        private void LoadResource(ResourceModel resource, string filename, bool isDefault = false)
        {
            switch (resource.Type)
            {
                case ResourceType.ICON0:
                case ResourceType.BOOT:
                case ResourceType.PIC1:
                case ResourceType.PIC0:
                    {
                        resource.Composite.Clear();

                        var appPath = ApplicationInfo.AppPath;

                        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);

                        BitmapSource image = ImageProcessing.GetBitmapImage(fileStream);

                        switch (resource.Type)
                        {
                            case ResourceType.ICON0:
                                {
                                    //image = ImageProcessing.Resize(image, 80, 80);


                                    if (_settings.GenerateIconFrame)
                                    {
                                        var alphaMaskUri = Path.Combine(appPath, "Resources", "alpha.png");
                                        using var maskStream = new FileStream(alphaMaskUri, FileMode.Open, FileAccess.Read);
                                        resource.Composite.SetAplhaMask(ImageProcessing.GetBitmapImage(maskStream));

                                        resource.Composite.Layers.Add(new ImageLayer(image, "image", alphaMaskUri));

                                        var overlayUri = Path.Combine(appPath, "Resources", "overlay.png");
                                        using var frameStream = new FileStream(overlayUri, FileMode.Open, FileAccess.Read);
                                        resource.Composite.Layers.Add(new ImageLayer(ImageProcessing.GetBitmapImage(frameStream), "frame", overlayUri));
                                    }
                                    else
                                    {
                                        resource.Composite.Layers.Add(new ImageLayer(image, "image", filename));
                                    }


                                    break;
                                }
                            case ResourceType.PIC0:
                                {
                                    //image = ImageProcessing.Resize(image, 310, 180);

                                    resource.Composite.Layers.Add(new ImageLayer(image, "image", filename));
                                    break;
                                }
                            case ResourceType.PIC1:
                            case ResourceType.BOOT:
                                {
                                    //image = ImageProcessing.Resize(image, 480, 272);

                                    resource.Composite.Layers.Add(new ImageLayer(image, "image", filename));
                                    break;
                                }
                        }

                        resource.RefreshIcon();
                        break;
                    }
                default:
                    {
                        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                        resource.CopyFromStream(fileStream);
                        break;
                    }
            }

            resource.IsLoadEnabled = true;
            resource.IsSaveAsEnabled = true;
            resource.IsRemoveEnabled = !isDefault;
            resource.SourceUrl = filename;
        }

        private void LoadResource_OnClick(object sender, RoutedEventArgs e)
        {
            var resource = (sender as MenuItem)!.DataContext as ResourceModel;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = _settings.LastResourceDirectory;
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(resource!.Type);

            var result = openFileDialog.ShowDialog();

            if (result is true)
            {
                _settings.LastResourceDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                LoadResource(resource, openFileDialog.FileName);
                Model.IsDirty = true;
            }

        }

        private void Resource_OnMore(object sender, RoutedEventArgs e)
        {
            var control = sender as ResourceControl;

            var cm = this.FindResource("ResourceButtonContextMenu") as ContextMenu;
            var menuItems = cm.Items.OfType<MenuItem>().Concat(cm.Items.OfType<Separator>().Cast<Control>());
            foreach (var menuItem in menuItems)
            {
                menuItem.DataContext = control.Resource;
                switch (menuItem.Name)
                {
                    case "LoadResource":
                        menuItem.IsEnabled = control.Resource.IsLoadEnabled;
                        break;
                    case "SaveResource":
                        menuItem.IsEnabled = control.Resource.IsSaveAsEnabled;
                        break;
                    case "TemplateSeparator":
                        menuItem.Visibility = control.Resource.IsTemplateEnabled ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case "LoadFromTemplate":
                        menuItem.Visibility = control.Resource.IsTemplateEnabled ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case "SaveAsTemplate":
                        menuItem.Visibility = control.Resource.IsTemplateEnabled ? Visibility.Visible : Visibility.Collapsed;
                        break;
                }
            }

            cm.PlacementTarget = control;
            cm.IsOpen = true;
        }

        private void SaveResource_OnClick(object sender, RoutedEventArgs e)
        {
            ResourceModel? resource = sender switch
            {
                ResourceControl resourceControl => resourceControl.Resource,
                MenuItem menu => menu.DataContext as ResourceModel,
                _ => null
            };

            if (resource != null)
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.InitialDirectory = _settings.LastResourceDirectory;
                saveFileDialog.AddExtension = true;
                saveFileDialog.Filter = ImageProcessing.GetFilterFromType(resource.Type);
                var result = saveFileDialog.ShowDialog();

                if (result is true)
                {
                    _settings.LastResourceDirectory = Path.GetDirectoryName(saveFileDialog.FileName);

                    using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                               FileAccess.Write))
                    {
                        resource.Stream!.Seek(0, SeekOrigin.Begin);
                        resource.Stream.CopyTo(output);
                        resource.Stream.Seek(0, SeekOrigin.Begin);
                        MessageBox.Show(Window, $"Resource has been extracted to \"{saveFileDialog.FileName}\"",
                            "PSXPackager",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void Resource_OnRemove(object sender, RoutedEventArgs e)
        {
            ResourceModel? resource = sender switch
            {
                ResourceControl resourceControl => resourceControl.Resource,
                MenuItem menu => menu.DataContext as ResourceModel,
                _ => null
            };

            if (resource != null)
            {
                switch (resource.Type)
                {
                    case ResourceType.ICON0:
                        LoadResource(Model.Icon0, GetDefaultResourceFile(Model.Icon0.Type), true);
                        break;
                    case ResourceType.PIC0:
                        LoadResource(Model.Pic0, GetDefaultResourceFile(Model.Pic0.Type), true);
                        break;
                    case ResourceType.PIC1:
                        LoadResource(Model.Pic1, GetDefaultResourceFile(Model.Pic1.Type), true);
                        break;
                    default:
                        resource.Clear();
                        break;
                }

                Model.IsDirty = true;
            }
        }

        private void ResetResource_OnClick(object sender, RoutedEventArgs e)
        {
            ResourceModel? resource = sender switch
            {
                ResourceControl resourceControl => resourceControl.Resource,
                MenuItem menu => menu.DataContext as ResourceModel,
                _ => null
            };

            if (resource != null)
            {
                switch (resource.Type)
                {
                    case ResourceType.ICON0:
                    case ResourceType.PIC0:
                    case ResourceType.PIC1:
                        LoadResource(resource, GetDefaultResourceFile(resource.Type), true);
                        break;
                    default:
                        resource.Clear();
                        break;
                }

                Model.IsDirty = true;
            }
        }

        private void SaveAsTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            ResourceModel? resource = sender switch
            {
                ResourceControl resourceControl => resourceControl.Resource,
                MenuItem menu => menu.DataContext as ResourceModel,
                _ => null
            };

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = _settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Template files|*.xml|All files|*.*";
            var result = saveFileDialog.ShowDialog();

            if (result is true)
            {

                var basePath = Path.GetDirectoryName(saveFileDialog.FileName);

                _settings.LastTemplateDirectory = basePath;

                var template = resource!.ToTemplateResource();

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

        private void LoadFromTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            ResourceModel? resource = sender switch
            {
                ResourceControl resourceControl => resourceControl.Resource,
                MenuItem menu => menu.DataContext as ResourceModel,
                _ => null
            };

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = _settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");

            openFileDialog.Filter = "Template files|*.xml|All files|*.*";

            var result = openFileDialog.ShowDialog();

            if (result is true)
            {
                var basePath = Path.GetDirectoryName(openFileDialog.FileName);

                _settings.LastTemplateDirectory = basePath;

                XmlSerializer serializer = new XmlSerializer(typeof(Resource));

                using (FileStream stream = File.OpenRead(openFileDialog.FileName))
                {
                    try
                    {
                        var xmlResource = (Resource)serializer.Deserialize(stream)!;
                        var resourceTemplate = xmlResource.ToResourceTemplate(basePath!);
                        if (resourceTemplate.ResourceType != resource.Type)
                        {
                            var confirmResult = MessageBox.Show(Window,
                                $"The selected template does not match the resource type {resource.Type}. Are you sure you want to continue?",
                                "PSXPackager",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning
                            );

                            if (confirmResult == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                        resource.Composite.Layers = new ObservableCollection<Layer>(resourceTemplate.Layers);
                        resource.RefreshIcon();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(Window, $"Failed to load template:\n{exception.Message}", "PSXPackager",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }


        }
    }
}
