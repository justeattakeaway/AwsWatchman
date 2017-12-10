using System;
using System.Linq;
using CommandLine;
using Watchman.Engine;

namespace Watchman
{
    public static class CommandLineParser
    {
        public static StartupParameters ToParameters(string[] args)
        {
            return Parser.Default.ParseArguments<StartupParameters>(args)
                .MapResult(
                    parsedFunc: startupParams =>
                    {
                        switch (startupParams.RunMode)
                        {
                            case RunMode.DryRun:
                            case RunMode.GenerateAlarms:
                            case RunMode.TestConfig:
                                return startupParams;
                            default:
                                Console.WriteLine("RunMode not recognised, exiting...");
                                return null;
                        }
                    },
                    notParsedFunc: _ =>
                    {
                        Console.WriteLine("Missing required arguments, exiting...");
                        return null;
                    });
        }
    }
}
