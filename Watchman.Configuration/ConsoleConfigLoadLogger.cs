using System;

namespace Watchman.Configuration
{
    public class ConsoleConfigLoadLogger : IConfigLoadLogger
    {
        private readonly bool _verbose;

        public ConsoleConfigLoadLogger(bool verbose)
        {
            _verbose = verbose;
        }

        public void Error(string message)
        {
            Console.WriteLine("Error: " + message);
        }

        public void Warn(string message)
        {
            Console.WriteLine("Warning: " + message);
        }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Detail(string message)
        {
            if (_verbose)
            {
                Console.WriteLine(message);
            }
        }
    }
}
