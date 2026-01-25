using System;
using System.IO;
using System.Threading.Tasks;
using Popstation.Pbp;

namespace Popstation
{
    /// <summary>
    /// A container for a PNG, PMF or AT3
    /// </summary>
    public class Resource : IDisposable
    {
        public Stream Stream { get; }

        /// <summary>
        /// The type of data that this <see cref="Resource"/> contains
        /// </summary>
        public ResourceType ResourceType { get; }

        /// <summary>
        /// The number of bytes allocated for this <see cref="Resource"/>
        /// </summary>
        public uint Size { get; }

        /// <summary>
        /// True if the source for this <see cref="Resource"/> has been set, False if the source file does not exist, or if this <see cref="Resource"/> is empty
        /// </summary>
        public bool Exists { get; }

        public Resource(ResourceType resourceType, Stream stream, uint size)
        {
            ResourceType = resourceType;
            Stream = stream;
            Size = size;
            Exists = true;
        }

        public Resource(ResourceType resourceType, byte[] buffer, uint size)
        {
            ResourceType = resourceType;
            Stream = new MemoryStream(buffer);
            Size = size;
            Exists = true;
        }

        private Resource(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        /// <summary>
        /// Creates an empty <see cref="Resource"/> of the specified type
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public static Resource Empty(ResourceType resourceType)
        {
            return new Resource(resourceType);
        }

        ///// <summary>
        ///// Writes the contents of this <see cref="Resource"/> to the specified Stream
        ///// </summary>
        ///// <param name="stream"></param>
        //public void Write(Stream stream)
        //{
        //    if (Buffer != null)
        //    {
        //        stream.Write(Buffer, 0, (int)Size);
        //        return;
        //    }

        //    Stream.Seek(0, SeekOrigin.Begin);
        //    Stream.CopyTo(stream);
        //}


        public void Dispose()
        {
            Stream?.Dispose();
        }

    }
}