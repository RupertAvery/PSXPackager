using System;
using System.IO;
using System.Threading.Tasks;
using Popstation.Pbp;

namespace Popstation
{

    /// <summary>
    /// Represents a data resource that encapsulates a stream or buffer along with its type and size information.
    /// </summary>
    /// <remarks>A Resource can be constructed from either a stream or a byte buffer, and provides access to
    /// the underlying data, its type, and its size. The Exists property indicates whether the resource has valid data.
    /// The Resource class implements IDisposable; callers should dispose of instances when they are no longer needed to
    /// release any associated stream resources.</remarks>
    public class Resource : IDisposable
    {
        public byte[] Buffer { get; private set; } = null;

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
            Buffer = buffer;
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

        public void Dispose()
        {
            Buffer = null;
            Stream?.Dispose();
        }

    }
}