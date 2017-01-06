using NUnit.Framework;
using QuarterMaster;

namespace Quartermaster.Tests
{
    [TestFixture]
    public class IocTests
    {
        [Test]
        public void TheReportGeneratorResovles()
        {
            var container = new IocBootstrapper()
                .ConfigureContainer(ValidStartupParameters());

            var generator = container.GetInstance<ReportGenerator>();

            Assert.That(generator, Is.Not.Null);
        }

        [Test]
        public void TheReportSenderResolves()
        {
            var container = new IocBootstrapper()
                .ConfigureContainer(ValidStartupParameters());

            var sender = container.GetInstance<ReportSender>();

            Assert.That(sender, Is.Not.Null);
        }

        private static StartupParameters ValidStartupParameters()
        {
            return new StartupParameters
            {
                AwsAccessKey = "a",
                AwsSecretKey = "b",
                ConfigFolderLocation = "c:\\temp"
            };
        }
    }
}
