using System;
using Moq;
using NUnit.Framework;
using Watchman.Configuration.Load;

namespace Watchman.Configuration.Tests.Load
{
    [TestFixture]
    public class DuplicatesTests
    {
        private Action GetLoader(string path)
        {
            return () =>
            {
                var testFilePath = TestFiles.GetRelativePathTo(path);
                var testFilesSettings = new FileSettings(testFilePath);

                var logger = new Mock<IConfigLoadLogger>();
                var loader = new ConfigLoader(testFilesSettings, logger.Object);

                loader.LoadConfig();
            };
        }

        [Test]
        public void LoadConfig_DuplicateDynamoBlocks_Throws()
        {
            var loader = GetLoader("data\\duplicates\\Dynamo");

            var caught = Assert.Throws<ConfigException>(() => loader());

            Assert.That(caught.InnerException, Is.Not.Null);
            Assert.That(caught.InnerException.Message, Is.EqualTo("DynamoDb block can only defined once"));
        }

        [Test]
        public void LoadConfig_DuplicateSqsBlocks_Throws()
        {
            var loader = GetLoader("data\\duplicates\\Sqs");

            var caught = Assert.Throws<ConfigException>(() => loader());

            Assert.That(caught.InnerException, Is.Not.Null);
            Assert.That(caught.InnerException.Message, Is.EqualTo("Sqs block can only defined once"));
        }
    }
}
