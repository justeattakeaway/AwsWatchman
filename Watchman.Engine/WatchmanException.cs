using System;

namespace Watchman.Engine
{
    public class WatchmanException : Exception
    {
        public WatchmanException()
        {
        }

        public WatchmanException(string message) : base(message)
        {
        }

        public WatchmanException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
