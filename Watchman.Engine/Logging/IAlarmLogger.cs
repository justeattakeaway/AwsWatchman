using System;

namespace Watchman.Engine.Logging
{
    public interface IAlarmLogger
    {
        void Error(Exception exception, string message);
        void Error(string message);
        void Info(string message);
        void Detail(string message);
    }
}
