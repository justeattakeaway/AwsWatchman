using Amazon;
using Watchman.Engine.Generation;
using Watchman.IoC;

namespace Watchman
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var startParams = CommandLineParser.ToParameters(args);
            if (startParams == null)
            {
                return ExitCode.InvalidParams;
            }

            return await GenerateAlarms(startParams);
        }

        private static async Task<int> GenerateAlarms(StartupParameters startParams)
        {
            try
            {
                if (startParams.AwsLogging)
                {
                    SetupAwsSdkLogging();
                }

                var container = new IocBootstrapper().ConfigureContainer(startParams);
                var alarmGenerator = container.GetInstance<AlarmLoaderAndGenerator>();

                await alarmGenerator.LoadAndGenerateAlarms(startParams.RunMode);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Run failed: {ex.Message}");
                return ExitCode.RunFailed;
            }
        }

        private static void SetupAwsSdkLogging()
        {
            var loggingConfig = AWSConfigs.LoggingConfig;
            loggingConfig.LogTo = LoggingOptions.Console;
            loggingConfig.LogMetrics = true;
            loggingConfig.LogResponses = ResponseLoggingOption.OnError;
            loggingConfig.LogResponsesSizeLimit = 4096;
            loggingConfig.LogMetricsFormat = LogMetricsFormatOption.JSON;
        }
    }
}
