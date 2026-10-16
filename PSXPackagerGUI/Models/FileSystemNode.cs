using System.Collections.ObjectModel;

namespace PSXPackagerGUI.Models
{

    public enum FileSystemNodeType
    {
        Directory,
        File
    }

    public class FileSystemNode : BaseNotifyModel
    {
        private ObservableCollection<FileSystemNode> _children = new ObservableCollection<FileSystemNode>();
        private string _path;
        private string _name;
        private int _size;
        private FileSystemNodeType _type;
        private ObservableCollection<FileSystemNode> _children1;

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public FileSystemNodeType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public ObservableCollection<FileSystemNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }
    }
}
