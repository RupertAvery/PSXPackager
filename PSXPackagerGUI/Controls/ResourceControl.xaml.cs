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
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource),
                typeof(ResourceControl),
                new FrameworkPropertyMetadata(OnIconChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string),
                typeof(ResourceControl));

        public static readonly RoutedEvent MoreEvent =
            EventManager.RegisterRoutedEvent(nameof(More), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly RoutedEvent RemoveEvent =
            EventManager.RegisterRoutedEvent(nameof(Remove), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly DependencyProperty IsMoreEnabledProperty =
            DependencyProperty.Register(nameof(IsMoreEnabled), typeof(bool),
                typeof(ResourceControl));

        public static readonly DependencyProperty IsRemoveEnabledProperty =
            DependencyProperty.Register(nameof(IsRemoveEnabled), typeof(bool),
                typeof(ResourceControl));

        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register(nameof(Resource), typeof(ResourceModel),
                typeof(ResourceControl),
                new FrameworkPropertyMetadata(OnResourceChanged));

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ResourceControl)d).Icon = (BitmapImage)e.NewValue;
        }

        private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ResourceControl)d).Resource = (ResourceModel)e.NewValue;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property.Name == nameof(Text))
            {
                LabelText.Content = e.NewValue;
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
            set { SetValue(ResourceProperty, value); DataContext = value; }
        }

        public bool IsMoreEnabled
        {
            get => (bool)GetValue(IsMoreEnabledProperty);
            set { SetValue(IsMoreEnabledProperty, value); MoreButton.IsEnabled = value; }
        }

        public bool IsRemoveEnabled
        {
            get => (bool)GetValue(IsRemoveEnabledProperty);
            set { SetValue(IsRemoveEnabledProperty, value); RemoveButton.IsEnabled = value; }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set { SetValue(TextProperty, value); LabelText.Content = value; }
        }

        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set { SetValue(IconProperty, value); }
        }

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
