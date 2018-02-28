using Amazon.CloudFormation;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Tests.Fakes;

namespace Watchman.Tests.IoC
{
    static class TestingIocBootstrapperExtensions
    {
        public static TestingIocBootstrapper WithConfig(this TestingIocBootstrapper bootstrapper, WatchmanConfiguration inputConfiguration)
        {
            bootstrapper.GetMock<IConfigLoader>().HasConfig(inputConfiguration);
            return bootstrapper;
        }

        public static TestingIocBootstrapper WithCloudFormation(this TestingIocBootstrapper bootstrapper, IAmazonCloudFormation cloudFormation)
        {
            bootstrapper.Override<IAmazonCloudFormation>(cloudFormation);
            return bootstrapper;
        }
    }
}
