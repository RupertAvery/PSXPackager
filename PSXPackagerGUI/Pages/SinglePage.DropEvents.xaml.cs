using PSXPackagerGUI.Common;
using System.IO;
using System.Linq;
using System.Windows;

namespace PSXPackagerGUI.Pages;

public partial class SinglePage
{


    private static string[] imageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };

    private void Icon0_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, imageExtensions, out var filename))
        {
            ResourceHelper.LoadResource(Model.Icon0, filename).WarnIfErrors();
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Icon1_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, imageExtensions, out var filename))
        {
            ResourceHelper.LoadResource(Model.Icon1, filename).WarnIfErrors(); 
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Pic0_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, imageExtensions, out var filename))
        {
            ResourceHelper.LoadResource(Model.Pic0, filename).WarnIfErrors();
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Pic1_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, imageExtensions, out var filename))
        {
            ResourceHelper.LoadResource(Model.Pic1, filename).WarnIfErrors();
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Boot_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, imageExtensions, out var filename))
        {
            ResourceHelper.LoadResource(Model.Boot, filename).WarnIfErrors();
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Snd0_OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetFilename(e.Data, new[] { ".at3" }, out var filename))
        {
            ResourceHelper.LoadResource(Model.Snd0, filename).WarnIfErrors();
            return;
        }
        MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private bool TryGetFilename(IDataObject data, string[] allowedExtensions, out string filename)
    {
        filename = ((string[])data.GetData("FileName"))[0];
        if (allowedExtensions.Contains(Path.GetExtension(filename).ToLower()))
        {
            return true;
        }
        return false;
    }

    private Stream GetImageStream(string filename)
    {
        return new FileStream(filename, FileMode.Open, FileAccess.Read);
    }

    private Stream GetDataStream(IDataObject data)
    {
        var filename = ((string[])data.GetData("FileName"))[0];
        return new FileStream(filename, FileMode.Open, FileAccess.Read);
    }
}