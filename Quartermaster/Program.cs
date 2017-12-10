using System;
using System.Threading.Tasks;
using CommandLine;
using QuarterMaster;

namespace Quartermaster
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
        {
            return Parser.Default.ParseArguments<StartupParameters>(args)
                .MapResult(
                    parsedFunc: GenerateReports,
                    notParsedFunc: _ =>
                    {
                        Console.WriteLine("Missing required arguments, exiting...");
                        return Task.FromResult(ExitCode.InvalidParams);
                    });
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
