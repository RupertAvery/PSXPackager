using System;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Thrown when a .chd file is malformed, truncated, or uses a feature this reader does not
    /// implement.
    /// </summary>
    public class InvalidChdException : Exception
    {
        public InvalidChdException(string message) : base(message)
        {
        }
    }
}
