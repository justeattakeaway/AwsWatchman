using System;
using System.Threading.Tasks;
using CommandLine;
using QuarterMaster;

namespace Quartermaster
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var startupParams = new StartupParameters();

            if (!Parser.Default.ParseArguments(args, startupParams))
            {
                Console.WriteLine("Missing required arguments, exiting...");
                return ExitCode.InvalidParams;
            }

            var task = GenerateReports(startupParams);
            task.Wait();
            return task.Result;
        }

        private static async Task<int> GenerateReports(StartupParameters startupParams)
        {
            try
            {
                var container = new IocBootstrapper().ConfigureContainer(startupParams);
                var reportGenerator = container.GetInstance<ReportGenerator>();
                var reports = await reportGenerator.GetReports();

                var reportSender = container.GetInstance<ReportSender>();
                await reportSender.SendReports(reports);

                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Run failed: {ex.Message}");
                return ExitCode.RunFailed;
            }
        }
    }
}
