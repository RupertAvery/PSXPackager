using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Controls
{
    /// <summary>
    /// Interaction logic for Resource.xaml
    /// </summary>
    public partial class ResourceControl : UserControl, INotifyPropertyChanged
    {
        //public static readonly DependencyProperty IconProperty =
        //    DependencyProperty.Register(nameof(Icon), typeof(ImageSource),
        //        typeof(ResourceControl),
        //        new FrameworkPropertyMetadata(OnIconChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string),
                typeof(ResourceControl));

        public static readonly RoutedEvent MoreEvent =
            EventManager.RegisterRoutedEvent(nameof(More), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly RoutedEvent RemoveEvent =
            EventManager.RegisterRoutedEvent(nameof(Remove), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        //public static readonly DependencyProperty IsMoreEnabledProperty =
        //    DependencyProperty.Register(nameof(IsMoreEnabled), typeof(bool),
        //        typeof(ResourceControl));

        //public static readonly DependencyProperty IsRemoveEnabledProperty =
        //    DependencyProperty.Register(nameof(IsRemoveEnabled), typeof(bool),
        //        typeof(ResourceControl));

        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register(nameof(Resource), typeof(ResourceModel),
                typeof(ResourceControl), new PropertyMetadata(null, OnResourceChanged));
        //new FrameworkPropertyMetadata(OnResourceChanged));


        //private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    ((ResourceControl)d).Icon = (BitmapImage)e.NewValue;
        //    ((ResourceControl)d).InvalidateVisual();
        //}

        private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = ((ResourceControl)d);
            control.Resource = (ResourceModel)e.NewValue;
            control.InvalidateVisual();
            control.Resource.PropertyChanged += ResourceOnPropertyChanged;

            void ResourceOnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
            {
                if (propertyChangedEventArgs.PropertyName == nameof(ResourceModel.Icon))
                {
                    control.InvalidateVisual();
                }
            }
        }
        
        public event RoutedEventHandler More
        {
            add => AddHandler(MoreEvent, value);
            remove => RemoveHandler(MoreEvent, value);
        }

        public event RoutedEventHandler Remove
        {
            add => AddHandler(RemoveEvent, value);
            remove => RemoveHandler(RemoveEvent, value);
        }

        public ResourceModel Resource
        {
            get => (ResourceModel)GetValue(ResourceProperty);
            set { SetValue(ResourceProperty, value); InvalidateVisual(); }
        }

        //public bool IsMoreEnabled
        //{
        //    get => (bool)GetValue(IsMoreEnabledProperty);
        //    set
        //    {
        //        SetValue(IsMoreEnabledProperty, value);
        //        OnPropertyChanged();
        //    }
        //}

        //public bool IsRemoveEnabled
        //{
        //    get => (bool)GetValue(IsRemoveEnabledProperty);
        //    set
        //    {
        //        SetValue(IsRemoveEnabledProperty, value); 
        //        OnPropertyChanged();
        //    }
        //}

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                SetValue(TextProperty, value);
                OnPropertyChanged();
            }
        }

        //public ImageSource Icon
        //{
        //    get => (ImageSource)GetValue(IconProperty);
        //    set
        //    {
        //        SetValue(IconProperty, value);
        //        OnPropertyChanged();
        //    }
        //}

        public ResourceControl()
        {
            InitializeComponent();
        }

        private void UIElement_OnDrop(object sender, DragEventArgs e)
        {

        }

        private void More_OnClick(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(MoreEvent, this);
            RaiseEvent(newEventArgs);
        }

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(RemoveEvent, this);
            RaiseEvent(newEventArgs);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
