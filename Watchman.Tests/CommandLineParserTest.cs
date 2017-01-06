using NUnit.Framework;
using Watchman.Engine;

namespace Watchman.Tests
{
    [TestFixture]
    public class CommandLineParserTest
    {
        [Test]
        public void TestNoParams()
        {
            var cmdLine = new string[0];

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Null);
        }

        [Test]
        public void DefaultModeIsDryRun()
        {
            var cmdLine = new[]
            {
                "--ConfigFolder", "c:\\foo"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.RunMode, Is.EqualTo(RunMode.DryRun));
        }

        [Test]
        public void UnrecognisedParamIsFailure()
        {
            var cmdLine = new[]
            {
                "--ConfigFolder", "c:\\foo",
                "--noSuchParam1234", "warrawakka"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Null);
        }

        [Test]
        public void FolderIsRead()
        {
            var cmdLine = new[]
            {
                "--ConfigFolder", "c:\\foo"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.ConfigFolderLocation, Is.EqualTo("c:\\foo"));
        }

        [Test]
        public void AwsDataIsRead()
        {
            var cmdLine = new[]
            {
                "--ConfigFolder", "c:\\foo",
                "--AwsAccessKey", "testKey",
                "--AwsSecretKey", "testSecret",
                "--AwsRegion", "testRegion"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.AwsAccessKey, Is.EqualTo("testKey"));
            Assert.That(startParams.AwsSecretKey, Is.EqualTo("testSecret"));
            Assert.That(startParams.AwsRegion, Is.EqualTo("testRegion"));
        }

        [Test]
        public void TestSuccessParamsForFullRun()
        {
            var cmdLine = new[]
            {
               "--ConfigFolder", "c:\\foo",
               "--RunMode", "GenerateAlarms"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.RunMode, Is.EqualTo(RunMode.GenerateAlarms));
        }

        [Test]
        public void TestSuccessParamsForRunModeTestConfigWithoutAwsCreds()
        {
            var cmdLine = new[]
            {
                "--RunMode", "TestConfig",
                "--ConfigFolder", "c:\\foo"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.RunMode, Is.EqualTo(RunMode.TestConfig));
        }

        [Test]
        public void TestSuccessParamsForRunModeDryRun()
        {
            var cmdLine = new[]
            {
                "--RunMode", "DryRun",
                "--AwsAccessKey", "testKey",
                "--AwsSecretKey", "testSecret",
                "--ConfigFolder", "c:\\foo"
            };

            var startParams = CommandLineParser.ToParameters(cmdLine);

            Assert.That(startParams, Is.Not.Null);
            Assert.That(startParams.AwsAccessKey, Is.EqualTo("testKey"));
            Assert.That(startParams.AwsSecretKey, Is.EqualTo("testSecret"));
        }
    }
}
