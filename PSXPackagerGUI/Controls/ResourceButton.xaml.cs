using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PSXPackagerGUI.Controls
{
    /// <summary>
    /// Interaction logic for ResourceButton.xaml
    /// </summary>
    public partial class ResourceButton : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked),
                typeof(bool),
                typeof(ResourceButton),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label),
                typeof(string),
                typeof(ResourceButton),
                new PropertyMetadata(null));
        
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive),
                typeof(bool),
                typeof(ResourceButton),
                new PropertyMetadata(false));

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(ResourceControl));

        public static readonly DependencyProperty IsFixedProperty =
            DependencyProperty.Register(nameof(IsFixed),
                typeof(bool),
                typeof(ResourceButton),
                new PropertyMetadata(false));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        public bool IsFixed
        {
            get => (bool)GetValue(IsFixedProperty);
            set
            {
                SetValue(IsFixedProperty, value);
                OnPropertyChanged();
            }
        }

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set
            {
                SetValue(IsCheckedProperty, value);
                OnPropertyChanged();
            }
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set
            {
                SetValue(LabelProperty, value);
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set
            {
                SetValue(IsActiveProperty, value);
                OnPropertyChanged();
            }
        }


        public ResourceButton()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var newEventArgs = new RoutedEventArgs(ClickEvent, this);
            RaiseEvent(newEventArgs);
        }
    }
}
