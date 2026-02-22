using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Xml.Serialization;
using Popstation.Pbp;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Pages;
using PSXPackagerGUI.Templates;
using ImageLayer = PSXPackagerGUI.Models.Resource.ImageLayer;
using Layer = PSXPackagerGUI.Models.Resource.Layer;

namespace PSXPackagerGUI.Common;


public static class ResourceResultExtensions
{
    public static void WarnIfErrors(this ResourceResult resourceResult)
    {
        if (!resourceResult.Success)
        {
            var messages = string.Join("\r\n", resourceResult.ErrorMessages);
            MessageBox.Show(Application.Current.MainWindow, $"An error occured while loading the resource or template for {resourceResult.ResourceType}.\r\n\r\n{messages}", "Resource load failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

public class ResourceResult
{
    public ResourceType ResourceType { get; set; }
    public bool Success => ErrorMessages.Count == 0;
    public List<string> ErrorMessages { get; set; } = new List<string>();
}

public class ResourceHelper
{
    public static ResourceResult LoadTemplate(ResourceModel resource, string filename, bool isDefault = false)
    {
        ResourceResult result = new ResourceResult() { ResourceType = resource.Type };

        try
        {
            switch (resource.Type)
            {
                case ResourceType.ICON0:
                case ResourceType.BOOT:
                case ResourceType.PIC1:
                case ResourceType.PIC0:
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Resource));

                        var basePath = Path.GetDirectoryName(filename);

                        using (FileStream stream = File.OpenRead(filename))
                        {
                            try
                            {
                                var xmlResource = (Resource)serializer.Deserialize(stream)!;
                                var (resourceTemplate, errorMessages) = xmlResource.ToResourceTemplate(basePath!);

                                if (resourceTemplate.ResourceType != resource.Type)
                                {
                                    result.ErrorMessages.Add("Resource did not match");
                                    return result;
                                }

                                resource.Composite.Clear();
                                resource.Composite.Layers = new ObservableCollection<Layer>(resourceTemplate.Layers);
                                resource.RefreshIcon();

                                if (errorMessages.Count > 0)
                                {
                                    result.ErrorMessages.AddRange(errorMessages);
                                }
                            }
                            finally
                            {

                            }
                        }
                        resource.HasResource = true;
                        break;
                    }
            }

        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load Template: {resource.Type} - {filename}", ex);
            result.ErrorMessages.Add(ex.Message);
        }

        return result;
    }


    public static ResourceResult LoadResource(ResourceModel resource, string filename, bool isDefault = false)
    {
        ResourceResult result = new ResourceResult() { ResourceType = resource.Type };

        try
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


                                    if (ServiceLocator.Settings.GenerateIconFrame)
                                    {
                                        var alphaMaskUri = Path.Combine(appPath, "Resources", "alpha.png");
                                        using var maskStream = new FileStream(alphaMaskUri, FileMode.Open, FileAccess.Read);
                                        resource.Composite.SetAplhaMask(ImageProcessing.GetBitmapImage(maskStream));


                                        var imageLayer = new ImageLayer(image, "image", filename);

                                        var width = imageLayer.Width - 12;
                                        var height = imageLayer.Height - 12;

                                        imageLayer.OffsetX = 6;
                                        imageLayer.OffsetY = 6;
                                        imageLayer.OriginalOffsetX = 6;
                                        imageLayer.OriginalOffsetY = 6;

                                        imageLayer.Width = width;
                                        imageLayer.Height = height;
                                        imageLayer.OriginalWidth = width;
                                        imageLayer.OriginalHeight = height;

                                        resource.Composite.Layers.Add(imageLayer);

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
                        resource.HasResource = true;
                        resource.SourceUrl = filename;
                        resource.Composite.Render();
                        resource.RefreshIcon();


                        break;
                    }
                default:
                    {
                        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                        resource.CopyFromStream(fileStream);
                        resource.HasResource = true;
                        resource.SourceUrl = filename;

                        break;
                    }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to load Resource: {resource.Type} - {filename}", e);
            result.ErrorMessages.Add(e.Message);
        }


        return result;
    }
}