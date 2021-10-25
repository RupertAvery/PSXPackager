using System.IO;
using Popstation.Pbp;

namespace Popstation
{
    public class Resource
    {
        private readonly string _file;
        private byte[] _buffer;
        public ResourceType ResourceType { get; }

        public uint Size { get; }

        public bool Exists { get; }

        public Resource(ResourceType resourceType, byte[] buffer, uint size)
        {
            ResourceType = resourceType;
            _buffer = buffer;
            Size = size;
            Exists = true;
        }

        public Resource(ResourceType resourceType, string file)
        {
            ResourceType = resourceType;
            _file = file;

            if (File.Exists(file))
            {
                var t = new FileInfo(file);
                Size = (uint)t.Length;
                Exists = true;
            }
            else
            {
                Size = 0;
            }
        }

        private Resource(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        public static Resource Empty(ResourceType resourceType)
        {
            return new Resource(resourceType);
        }

        public void Write(Stream stream)
        {
            if (_file != null)
            {
                var buffer = new byte[Size];
                using (var t = new FileStream(_file, FileMode.Open, FileAccess.Read))
                {
                    t.Read(buffer, 0, (int)Size);
                    stream.Write(buffer, 0, (int)Size);
                }
            }
            else
            {
                stream.Write(_buffer, 0, (int)Size);
            }
        }

    }
}