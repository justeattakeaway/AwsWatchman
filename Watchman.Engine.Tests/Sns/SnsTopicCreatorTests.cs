using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using NSubstitute;
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
            var client = Substitute.For<IAmazonSimpleNotificationService>();
            MockCreateTopic(client, TestCreateTopicResponse());

            var logger = Substitute.For<IAlarmLogger>();
            var snsTopicCreator = new SnsTopicCreator(client, logger);

            var topicArn = await snsTopicCreator.EnsureSnsTopic("test1", false);

            Assert.That(topicArn, Is.Not.Null);
            Assert.That(topicArn, Is.EqualTo("testResponse-abc123"));

            await client.Received(1).CreateTopicAsync("test1-Alerts", Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task DryRunShouldNotCreateTopic()
        {
            var client = Substitute.For<IAmazonSimpleNotificationService>();
            MockCreateTopic(client, TestCreateTopicResponse());

            var logger = Substitute.For<IAlarmLogger>();
            var snsTopicCreator = new SnsTopicCreator(client, logger);

            var topicArn = await snsTopicCreator.EnsureSnsTopic("test1", true);

            Assert.That(topicArn, Is.Not.Null);

            await client.DidNotReceiveWithAnyArgs().CreateTopicAsync(default(string));
        }

        private static CreateTopicResponse TestCreateTopicResponse()
        {
            return new CreateTopicResponse
            {
                TopicArn = "testResponse-abc123"
            };
        }

        private void MockCreateTopic(IAmazonSimpleNotificationService client,
            CreateTopicResponse response)
        {
            client
                .CreateTopicAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(response);
        }
    }
}
