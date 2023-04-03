using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Moq;
using NUnit.Framework;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Tests.Sns
{
    [TestFixture]
    public class SnsTopicCreatorTests
    {
        [Test]
        public async Task HappyPathShouldCreateTopic()
        {
            var client = new Mock<IAmazonSimpleNotificationService>();
            MockCreateTopic(client, TestCreateTopicResponse());

            var logger = new Mock<IAlarmLogger>();
            var snsTopicCreator = new SnsTopicCreator(client.Object, logger.Object);

            var topicArn = await snsTopicCreator.EnsureSnsTopic("test1", false);

            Assert.That(topicArn, Is.Not.Null);
            Assert.That(topicArn, Is.EqualTo("testResponse-abc123"));

            client.Verify(c => c.CreateTopicAsync("test1-Alerts", It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task DryRunShouldNotCreateTopic()
        {
            var client = new Mock<IAmazonSimpleNotificationService>();
            MockCreateTopic(client, TestCreateTopicResponse());

            var logger = new Mock<IAlarmLogger>();
            var snsTopicCreator = new SnsTopicCreator(client.Object, logger.Object);

            var topicArn = await snsTopicCreator.EnsureSnsTopic("test1", true);

            Assert.That(topicArn, Is.Not.Null);

            client.Verify(c => c.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static CreateTopicResponse TestCreateTopicResponse()
        {
            return new CreateTopicResponse
            {
                TopicArn = "testResponse-abc123"
            };
        }
        
        private void MockCreateTopic(Mock<IAmazonSimpleNotificationService> client,
            CreateTopicResponse response)
        {
            client
                .Setup(c => c.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
