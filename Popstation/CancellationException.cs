using System;

namespace Popstation
{
    public class CancellationException : Exception
    {
        public CancellationException(string message) : base(message)
        {

        }
    }
}