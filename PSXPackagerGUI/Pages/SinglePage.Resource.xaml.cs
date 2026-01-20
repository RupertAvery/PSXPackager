using Popstation.Pbp;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Controls;
using PSXPackagerGUI.Models.Resource;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using PSXPackagerGUI.Templates;
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
            var resource = (sender as MenuItem).DataContext as ResourceModel;

            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();

            openFileDialog.Filter = ImageProcessing.GetFilterFromType(resource.Type);

            openFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(openFileDialog.FileName))
            {
                LoadResource(resource, openFileDialog.FileName);
                Model.IsDirty = true;
            }

        }

        private void Resource_OnMore(object sender, RoutedEventArgs e)
        {
            var control = sender as ResourceControl;

            var cm = this.FindResource("ResourceButtonContextMenu") as ContextMenu;
            var menuItems = cm.Items.OfType<MenuItem>();
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
                var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
                saveFileDialog.AddExtension = true;
                saveFileDialog.Filter = ImageProcessing.GetFilterFromType(resource.Type);
                saveFileDialog.ShowDialog();

                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                               FileAccess.Write))
                    {
                        resource.Stream.Seek(0, SeekOrigin.Begin);
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

            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Template files|*.xml|All files|*.*";
            saveFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                var basePath = Path.GetDirectoryName(saveFileDialog.FileName);

                var template = resource.Composite.ToTemplateResource();

                foreach (var layer in template.Layers)
                {
                    if (layer is Templates.ImageLayer imageLayer)
                    {
                        if (Path.GetDirectoryName(imageLayer.SourceUri) == basePath)
                        {
                            imageLayer.SourceUri = Path.GetRelativePath(basePath, imageLayer.SourceUri);
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

            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();

            openFileDialog.Filter = "Template files|*.xml|All files|*.*";

            openFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(openFileDialog.FileName))
            {
                var basePath = Path.GetDirectoryName(openFileDialog.FileName);

                XmlSerializer serializer = new XmlSerializer(typeof(Resource));

                using (FileStream stream = File.OpenRead(openFileDialog.FileName))
                {
                    var xmlResource = (Resource)serializer.Deserialize(stream);

                    resource.Composite.Layers = new ObservableCollection<Layer>(xmlResource.ToLayers(basePath));
                    resource.RefreshIcon();
                }
            }

     
        }
    }
}
