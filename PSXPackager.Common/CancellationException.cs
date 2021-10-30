using System;

namespace PSXPackager.Common
{
    public class CancellationException : Exception
    {
        public CancellationException(string message) : base(message)
        {

        }
    }
}