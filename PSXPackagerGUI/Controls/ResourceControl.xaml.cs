using PSXPackagerGUI.Models.Resource;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

    }
}
