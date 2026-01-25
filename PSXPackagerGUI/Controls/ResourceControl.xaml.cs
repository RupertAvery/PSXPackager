using Microsoft.VisualBasic;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Pages;
using System.ComponentModel;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Controls
{
    /// <summary>
    /// Interaction logic for Resource.xaml
    /// </summary>
    public partial class ResourceControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register(nameof(Resource),
                typeof(ResourceModel),
                typeof(ResourceControl),
                new PropertyMetadata(null, OnResourceChanged));

        private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ResourceControl)d;

            // Optional: access old/new values
            var oldValue = (ResourceModel)e.OldValue;
            var newValue = (ResourceModel)e.NewValue;

            //var control = ((ResourceControl)d);

            //control.Resource = (ResourceModel)e.NewValue;
            //control.InvalidateVisual();
            //control.Resource.PropertyChanged += ResourceOnPropertyChanged;

            //void ResourceOnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
            //{
            //    if (propertyChangedEventArgs.PropertyName == nameof(ResourceModel.Icon))
            //    {
            //        //control.InvalidateVisual();
            //    }
            //}


            newValue.RefreshIcon();
        }

        public SettingsModel Settings => ServiceLocator.Settings;

        public ResourceModel Resource
        {
            get => (ResourceModel)GetValue(ResourceProperty);
            set
            {
                SetValue(ResourceProperty, value);
                OnPropertyChanged();
            }
        }


        public ResourceControl()
        {
            InitializeComponent();
            //SizeChanged += (s, e) => UpdateSelection();
        }

        private void More_OnClick(object sender, RoutedEventArgs e)
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ImageEditorControl_OnUpdated(object sender, RoutedEventArgs e)
        {
            Resource.RefreshIcon();
        }

        private void ImageEditorControl_OnSave(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = Settings.LastResourceDirectory;
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);
            var result = saveFileDialog.ShowDialog();

            if (result is true)
            {
                Settings.LastResourceDirectory = Path.GetDirectoryName(saveFileDialog.FileName);

                using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                           FileAccess.Write))
                {
                    Resource.Stream!.Seek(0, SeekOrigin.Begin);
                    Resource.Stream.CopyTo(output);
                    Resource.Stream.Seek(0, SeekOrigin.Begin);
                    MessageBox.Show(App.Current.MainWindow, $"Resource has been extracted to \"{saveFileDialog.FileName}\"",
                        "PSXPackager",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ImageEditorControl_OnLoad(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = Settings.LastResourceDirectory;
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

            var result = openFileDialog.ShowDialog();

            if (result is true)
            {
                Settings.LastResourceDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                ResourceHelper.LoadResource(Resource, openFileDialog.FileName);
            }
        }

        private void LoadResource_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = Settings.LastResourceDirectory;
            openFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);

            var result = openFileDialog.ShowDialog();

            if (result is true)
            {
                Settings.LastResourceDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                ResourceHelper.LoadResource(Resource, openFileDialog.FileName);
            }
        }

        private void SaveResource_OnClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = Settings.LastResourceDirectory;
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = ImageProcessing.GetFilterFromType(Resource.Type);
            var result = saveFileDialog.ShowDialog();

            if (result is true)
            {
                Settings.LastResourceDirectory = Path.GetDirectoryName(saveFileDialog.FileName);

                using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                           FileAccess.Write))
                {
                    Resource.Stream!.Seek(0, SeekOrigin.Begin);
                    Resource.Stream.CopyTo(output);
                    Resource.Stream.Seek(0, SeekOrigin.Begin);
                    MessageBox.Show(App.Current.MainWindow, $"Resource has been extracted to \"{saveFileDialog.FileName}\"",
                        "PSXPackager",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
