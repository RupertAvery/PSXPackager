using System;
using System.IO;
using System.Windows.Media;
using Popstation.Pbp;

namespace PSXPackagerGUI.Models.Resource
{
    public class ResourceModel : BaseNotifyModel
    {
        private Stream? _stream;
        private ImageComposite? _composite;
        private ImageSource? _icon;
        private bool _text;
        private bool _isIncluded;

        public event EventHandler Cleared;

        public bool IsIncluded
        {
            get => _isIncluded;
            set => SetProperty(ref _isIncluded, value);
        }

        public ImageComposite? Composite
        {
            get => _composite;
            set => SetProperty(ref _composite, value);
        }

        public ResourceModel()
        {
        }

        public static ResourceModel OtherResource(ResourceType type)
        {
            var resource = new ResourceModel
            {
                Type = type
            };
            return resource;
        }

        public static ResourceModel ImageResource(ResourceType type, int width, int height)
        {
            var resource = new ResourceModel
            {
                Type = type,
                Composite = new ImageComposite(width, height),
                IsTemplateEnabled = true
            };
            return resource;
        }

        public void RefreshIcon()
        {
            Icon = Composite?.CompositeBitmap;
        }

        public void Reset()
        {
            Composite?.Clear();
            Cleared?.Invoke(this, EventArgs.Empty);
            Icon = null;
            SourceUrl = null;
            Stream?.Dispose();
            IsIncluded = false;
            HasResource = false;
        }

        public ResourceType Type { get; set; }

        public ImageSource? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public bool Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string? SourceUrl { get; set; }

        public Stream? Stream
        {
            get
            {
                if (Composite != null && Composite.IsDirty)
                {
                    _stream?.Dispose();
                    _stream = new MemoryStream();
                    Composite.SaveToPNG(_stream);
                    _stream.Seek(0, SeekOrigin.Begin);
                    Size = (uint)_stream.Length;
                    Composite.SetPristine();
                }
                return _stream;
            }
            private set
            {
                _stream = value;
            }
        }

        public uint Size { get; private set; }
        public bool IsTemplateEnabled { get; set; }
        public bool HasResource { get; set; }

        public void Clear()
        {
            Stream?.Dispose();
            Stream = null;
            Size = 0;
            SourceUrl = null;
            Icon = null;
        }

        //public void FromStream(Stream stream)
        //{
        //    Stream?.Dispose();
        //    Stream = stream;
        //    Stream.Seek(0, SeekOrigin.Begin);
        //    Size = (uint)Stream.Length;
        //}

        public void CopyFromStream(Stream stream)
        {
            Stream?.Dispose();
            Stream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(Stream);
            Stream.Seek(0, SeekOrigin.Begin);
            Size = (uint)Stream.Length;
        }

    }
}