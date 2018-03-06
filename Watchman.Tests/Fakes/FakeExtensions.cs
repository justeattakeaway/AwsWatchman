using Moq;
using Watchman.Configuration;
using Watchman.Configuration.Load;

namespace Watchman.Tests.Fakes
{
    static class FakeExtensions
    {
        public static void HasConfig(this Mock<IConfigLoader> loader, WatchmanConfiguration config)
        {
            loader.Setup(x => x.LoadConfig()).Returns(config);
        }
    }
}
