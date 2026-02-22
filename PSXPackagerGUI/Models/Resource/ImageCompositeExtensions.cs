using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Popstation.Pbp;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI.Models.Resource;

public static class ImageCompositeExtensions
{
    public static Templates.Resource ToTemplateResource(this ResourceTemplate resourceTemplate)
    {
        // Convert ImageComposite back to Templates.Resource
        return new Templates.Resource()
        {
            ResourceType = resourceTemplate.ResourceType,
            Width = resourceTemplate.Width,
            Height = resourceTemplate.Height,
            Layers = resourceTemplate.Layers.Select(layer =>
            {
                return layer switch
                {
                    ImageLayer imgLayer => new Templates.ImageLayer()
                    {
                        X = imgLayer.OffsetX,
                        Y = imgLayer.OffsetY,
                        Width = imgLayer.Width,
                        Height = imgLayer.Height,
                        OriginalWidth = imgLayer.OriginalWidth,
                        OriginalHeight = imgLayer.OriginalHeight,
                        SourceUri = imgLayer.SourceUri
                    } as Templates.Layer,
                    TextLayer textLayer => new Templates.TextLayer()
                    {
                        X = textLayer.OffsetX,
                        Y = textLayer.OffsetY,
                        Width = textLayer.Width,
                        Height = textLayer.Height,
                        Text = textLayer.TextContent,
                        FontFamily = textLayer.FontFamily.Source,
                        FontSize = textLayer.FontSize,
                        Color = textLayer.Color.ToString(),
                        DropShadow = textLayer.DropShadow,
                        OriginalWidth = textLayer.OriginalWidth,
                        OriginalHeight = textLayer.OriginalHeight
                    } as Templates.Layer,
                    _ => throw new NotSupportedException("Unsupported layer type")
                };
            }).ToList()
        };

    }

    public static (ResourceTemplate, List<string>) ToResourceTemplate(this Templates.Resource resource, string basePath)
    {
        var layers = new List<Layer>();

        var errorMessages = new List<string>();

        if (resource.Layers != null)
        {
            foreach (var layer in resource.Layers)
            {
                if (layer is Templates.ImageLayer imgLayer)
                {
                    if (!Path.IsPathFullyQualified(imgLayer.SourceUri))
                    {
                        imgLayer.SourceUri = Path.Combine(basePath, imgLayer.SourceUri);
                    }

                    try
                    {
                        var stream = new FileStream(imgLayer.SourceUri, FileMode.Open, FileAccess.Read);
                        var bitmap = ImageProcessing.GetBitmapImage(stream);
                        var imageLayer = new ImageLayer(bitmap, "image", imgLayer.SourceUri)
                        {
                            Bitmap = bitmap,
                            OffsetX = imgLayer.X,
                            OffsetY = imgLayer.Y,
                            Width = imgLayer.Width,
                            Height = imgLayer.Height,
                            OriginalOffsetX = imgLayer.X,
                            OriginalOffsetY = imgLayer.Y,
                            OriginalWidth = imgLayer.Width,
                            OriginalHeight = imgLayer.Height,
                        };
                        layers.Add(imageLayer);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to load image {imgLayer.SourceUri} for resource {resource.ResourceType}", e);
                        errorMessages.Add(e.Message);
                    }
                }
                else if (layer is Templates.TextLayer textLayer)
                {
                    try
                    {
                        var textLayerModel = new TextLayer("text",
                            textLayer.Text,
                            FontManager.GetFontFamily(textLayer.FontFamily),
                            textLayer.FontSize,
                            new SolidColorBrush((Color)ColorConverter.ConvertFromString(textLayer.Color)),
                            textLayer.DropShadow,
                            (int)textLayer.OriginalWidth,
                            (int)textLayer.OriginalHeight)
                        {
                            OffsetX = textLayer.X,
                            OffsetY = textLayer.Y,
                            Width = textLayer.Width,
                            Height = textLayer.Height,
                            OriginalOffsetX = textLayer.X,
                            OriginalOffsetY = textLayer.Y,
                            OriginalWidth = textLayer.Width,
                            OriginalHeight = textLayer.Height,
                        };
                        layers.Add(textLayerModel);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to load text layer for resource {resource.ResourceType}", e);
                        errorMessages.Add(e.Message);
                    }

                }
            }
        }


        return (new ResourceTemplate()
        {
            ResourceType = resource.ResourceType,
            Width = resource.Width,
            Height = resource.Height,
            Layers = layers
        }, errorMessages);
    }
}