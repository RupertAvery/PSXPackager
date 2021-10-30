namespace PSXPackagerGUI
{
    public class MainModel : BaseNotifyModel
    {
        private AppMode _mode;
        private bool _isDirty;

        public AppMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }
    }
}