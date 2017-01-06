using System;
using CommandLine;
using Watchman.Engine;

namespace Watchman
{
    public static class CommandLineParser
    {
        public static StartupParameters ToParameters(string[] args)
        {
            var startupParams = new StartupParameters();

            if (!Parser.Default.ParseArguments(args, startupParams))
            {
                Console.WriteLine("Missing required arguments, exiting...");
                return null;
            }

            switch (startupParams.RunMode)
            {
                case RunMode.DryRun:
                case RunMode.GenerateAlarms:
                case RunMode.TestConfig:
                    break;

                default:
                    Console.WriteLine("RunMode not recognised, exiting...");
                    return null;
            }

            return startupParams;
        }
    }
}
