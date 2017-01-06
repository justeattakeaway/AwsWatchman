using NUnit.Framework;
using Watchman.Configuration.Validation;

namespace Watchman.Configuration.Tests.Validation
{
    public static class ConfigAssert
    {
        public static void IsValid(WatchmanConfiguration config)
        {
            ConfigValidator.Validate(config);
            Assert.That(config, Is.Not.Null);
        }

        public static void NotValid(WatchmanConfiguration config, string expectedMessage)
        {
            var ex = Assert.Throws<ConfigException>(() => ConfigValidator.Validate(config));
            Assert.That(ex.Message, Is.EqualTo(expectedMessage));
        }
    }
}
