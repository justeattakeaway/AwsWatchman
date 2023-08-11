using NSubstitute;
using NUnit.Framework;
using Watchman.Configuration.Load;
using Watchman.Configuration.Validation;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class DuplicatesTests
    {
        private Func<WatchmanConfiguration> GetLoader(string path)
        {
            return () =>
            {
                var testFilePath = TestFiles.GetRelativePathTo(path);
                var testFilesSettings = new FileSettings(testFilePath);

                var logger = Substitute.For<IConfigLoadLogger>();
                var loader = new ConfigLoader(testFilesSettings, logger);

                return loader.LoadConfig();
            };
        }

        [Test]
        public void LoadConfig_DuplicateGroupNames_Throws()
        {
            var loader = GetLoader(Path.Join("data", "duplicates", "duplicateGroups"));

            var config = loader();

            var caught = Assert.Throws<ConfigException>(() => ConfigValidator.Validate(config));

            Assert.That(caught, Is.Not.Null);
            Assert.That(caught.Message, Contains.Substring("The following alerting group names exist in multiple config files"));
            Assert.That(caught.Message, Contains.Substring("TestGroupName"));
        }
    }
}
