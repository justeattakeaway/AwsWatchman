namespace Watchman.Configuration
{
    public interface IConfigLoadLogger
    {
        void Error(string message);
        void Warn(string message);
        void Info(string message);
        void Detail(string message);
    }
}
