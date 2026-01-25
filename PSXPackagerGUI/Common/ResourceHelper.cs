using System.Collections.ObjectModel;
using System;
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

public class ResourceHelper
{

    public static void LoadTemplate(ResourceModel resource, string filename, bool isDefault = false)
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
                            var resourceTemplate = xmlResource.ToResourceTemplate(basePath!);
                            if (resourceTemplate.ResourceType != resource.Type)
                            {
                                return;
                            }
                            resource.Composite.Clear();
                            resource.Composite.Layers = new ObservableCollection<Layer>(resourceTemplate.Layers);
                            resource.RefreshIcon();
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

    public static void LoadResource(ResourceModel resource, string filename, bool isDefault = false)
    {
        switch (resource.Type)
        {
            case ResourceType.ICON0:
            case ResourceType.BOOT:
            case ResourceType.PIC1:
            case ResourceType.PIC0:
                {
                    resource.Composite.Clear();

                    try
                    {
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
                        resource.HasResource = true;
                        resource.SourceUrl = filename;
                        resource.RefreshIcon();
                    }
                    catch (Exception e)
                    {
                    }

                    break;
                }
            default:
                {
                    try
                    {
                        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                        resource.CopyFromStream(fileStream);
                        resource.HasResource = true;
                        resource.SourceUrl = filename;
                    }
                    catch (Exception e)
                    {
                    }
                    break;
                }
        }

    }
}