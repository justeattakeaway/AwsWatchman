using System;

namespace Watchman.Engine.Alarms
{
    public class AlarmThresholds
    {
        public static double Calulate(long currentCapacity, double alertingThreshold)
        {
            return currentCapacity * AwsConstants.FiveMinutesInSeconds * alertingThreshold;
        }

        public static bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }
    }
}
