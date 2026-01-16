using System.Windows.Input;

namespace PSXPackagerGUI.Models
{
    public class Disc : BaseNotifyModel
    {
        private string _title;
        private string _saveTitle;
        private string _gameId;
        private string _saveId;
        private uint _size;
        private bool _isEmpty;
        private bool _isRemoveEnabled;
        private bool _isSaveAsEnabled;
        private bool _isLoadEnabled;
        private ICommand _removeCommand;

        public int Index { get; set; }

        public Disc()
        {
            Index = -1;
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SaveTitle
        {
            get => _saveTitle;
            set => SetProperty(ref _saveTitle, value);
        }

        public ICommand LoadCommand { get; set; }
        public ICommand SaveAsCommand { get; set; }

        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set => SetProperty(ref _removeCommand, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        public bool IsLoadEnabled
        {
            get => _isLoadEnabled;
            set => SetProperty(ref _isLoadEnabled, value);
        }

        public bool IsSaveAsEnabled
        {
            get => _isSaveAsEnabled;
            set => SetProperty(ref _isSaveAsEnabled, value);
        }

        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set => SetProperty(ref _isRemoveEnabled, value);
        }

        public uint Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string GameID
        {
            get => _gameId;
            set => SetProperty(ref _gameId, value);
        }

        public string SaveID
        {
            get => _saveId;
            set => SetProperty(ref _saveId, value);
        }


        public string SourceUrl { get; set; }
        public string SourceTOC { get; set; }

        public static Disc EmptyDisc(int index)
        {
            return new Disc()
            {
                Index = index,
                Title = "No Disc",
                IsEmpty = true,
                IsRemoveEnabled = false,
                IsLoadEnabled = true,
                IsSaveAsEnabled = false,
            };
        }
    }

}