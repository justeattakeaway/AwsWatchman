namespace Watchman.Engine.Logging
{
    public class ConsoleAlarmLogger : IAlarmLogger
    {
        private readonly bool _verbose;

        public ConsoleAlarmLogger(bool verbose)
        {
            _verbose = verbose;
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(Exception ex, string message)
        {
            Console.WriteLine($"{message} : {ex.Message}");
            Console.WriteLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                Error(ex.InnerException, "Inner " + message);
            }
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
