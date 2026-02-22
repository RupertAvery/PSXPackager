using Microsoft.Win32;
using Popstation.Pbp;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Common;
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
    public partial class SinglePage 
    {

        

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
                ResourceHelper.LoadResource(resource, openFileDialog.FileName).WarnIfErrors(); 
                Model.IsDirty = true;
            }

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
                        ResourceHelper.LoadResource(Model.Icon0, GetDefaultResourceFile(Model.Icon0.Type), true);
                        break;
                    case ResourceType.PIC0:
                        ResourceHelper.LoadResource(Model.Pic0, GetDefaultResourceFile(Model.Pic0.Type), true);
                        break;
                    case ResourceType.PIC1:
                        ResourceHelper.LoadResource(Model.Pic1, GetDefaultResourceFile(Model.Pic1.Type), true);
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
                        ResourceHelper.LoadResource(resource, GetDefaultResourceFile(resource.Type), true);
                        break;
                    default:
                        resource.Clear();
                        break;
                }

                Model.IsDirty = true;
            }
        }

        //private void SaveAsTemplate_OnClick(object sender, RoutedEventArgs e)
        //{
        //    ResourceModel? resource = sender switch
        //    {
        //        ResourceControl resourceControl => resourceControl.Resource,
        //        MenuItem menu => menu.DataContext as ResourceModel,
        //        _ => null
        //    };

        //    var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
        //    saveFileDialog.InitialDirectory = _settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");
        //    saveFileDialog.AddExtension = true;
        //    saveFileDialog.Filter = "Template files|*.xml|All files|*.*";
        //    var result = saveFileDialog.ShowDialog();

        //    if (result is true)
        //    {

        //        var basePath = Path.GetDirectoryName(saveFileDialog.FileName);

        //        _settings.LastTemplateDirectory = basePath;

        //        var template = resource!.ToTemplateResource();

        //        foreach (var layer in template.Layers)
        //        {
        //            if (layer is Templates.ImageLayer imageLayer)
        //            {
        //                if (Path.GetDirectoryName(imageLayer.SourceUri) == basePath)
        //                {
        //                    imageLayer.SourceUri = Path.GetRelativePath(basePath!, imageLayer.SourceUri);
        //                }
        //            }
        //        }

        //        XmlSerializer serializer = new XmlSerializer(typeof(Resource));
        //        using (TextWriter writer = new StreamWriter(saveFileDialog.FileName))
        //        {
        //            serializer.Serialize(writer, template);
        //        }
        //    }
        //}

        //private void LoadFromTemplate_OnClick(object sender, RoutedEventArgs e)
        //{
        //    ResourceModel? resource = sender switch
        //    {
        //        ResourceControl resourceControl => resourceControl.Resource,
        //        MenuItem menu => menu.DataContext as ResourceModel,
        //        _ => null
        //    };

        //    var openFileDialog = new Microsoft.Win32.OpenFileDialog();
        //    openFileDialog.InitialDirectory = _settings.LastTemplateDirectory ?? Path.Combine(ApplicationInfo.AppPath, "Templates");

        //    openFileDialog.Filter = "Template files|*.xml|All files|*.*";

        //    var result = openFileDialog.ShowDialog();

        //    if (result is true)
        //    {
        //        var basePath = Path.GetDirectoryName(openFileDialog.FileName);

        //        _settings.LastTemplateDirectory = basePath;

        //        XmlSerializer serializer = new XmlSerializer(typeof(Resource));

        //        using (FileStream stream = File.OpenRead(openFileDialog.FileName))
        //        {
        //            try
        //            {
        //                var xmlResource = (Resource)serializer.Deserialize(stream)!;
        //                var resourceTemplate = xmlResource.ToResourceTemplate(basePath!);
        //                if (resourceTemplate.ResourceType != resource.Type)
        //                {
        //                    var confirmResult = MessageBox.Show(Window,
        //                        $"The selected template does not match the resource type {resource.Type}. Are you sure you want to continue?",
        //                        "PSXPackager",
        //                        MessageBoxButton.YesNo,
        //                        MessageBoxImage.Warning
        //                    );

        //                    if (confirmResult == MessageBoxResult.No)
        //                    {
        //                        return;
        //                    }
        //                }
        //                resource.Composite.Layers = new ObservableCollection<Layer>(resourceTemplate.Layers);
        //                resource.RefreshIcon();
        //            }
        //            catch (Exception exception)
        //            {
        //                MessageBox.Show(Window, $"Failed to load template:\n{exception.Message}", "PSXPackager",
        //                    MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }
        //    }


        //}
    }
}
