using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Moq;
using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Tests.Sns
{
    [TestFixture]
    public class SnsCreatorTests
    {
        [Test]
        public async Task ReturnsArnForUrlTarget()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertUrl(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task UrlSubscriptionIsAdded()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var config = ConfigWithAlertUrl();
            var urlSub = (AlertUrl)config.Targets[0];

            await sut.EnsureSnsTopic(config, false);

            VerifyTopicCreated(sns);
            VerifyHttpSubscriptionAdded(sns, "sns-topic-arn", urlSub.Url);
        }

        [Test]
        public async Task EmailSubscriptionIsAdded()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var config = ConfigWithAlertEmail();
            var email = (AlertEmail)config.Targets[0];

            await sut.EnsureSnsTopic(config, false);

            VerifyTopicCreated(sns);
            VerifyEmailSubscriptionAdded(sns, "sns-topic-arn", email.Email);
        }

        [Test]
        public async Task ReturnsArnForEmailTarget()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertEmail(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task ReturnsArnForBothTargets()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithBoth(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task ReturnsArnInDryRunMode()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertUrl(), true);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task NoSubscriptionAddedInDryRunMode()
        {
            var sns = new Mock<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            await sut.EnsureSnsTopic(ConfigWithAlertUrl(), true);

            VerifyNoTopicCreated(sns);
            VerifyNoSubscriptionAdded(sns);
        }

        private static SnsCreator MakeSnsCreator(Mock<IAmazonSimpleNotificationService> sns)
        {
            var logger = new ConsoleAlarmLogger(false);

            return new SnsCreator(
                new SnsTopicCreator(sns.Object, logger),
                new SnsSubscriptionCreator(logger, sns.Object));
        }

        private AlertingGroup ConfigWithAlertUrl()
        {
            return new AlertingGroup
            {
                Name = "TestGroup",
                Targets = new List<AlertTarget>
                {
                    new AlertUrl("http://foo.bar.com")
                }
            };
        }

        private AlertingGroup ConfigWithAlertEmail()
        {
            return new AlertingGroup
            {
                Name = "TestGroup",
                Targets = new List<AlertTarget>
                {
                    new AlertEmail("foo@bar.com")
                }
            };
        }

        private AlertingGroup ConfigWithBoth()
        {
            return new AlertingGroup
            {
                Name = "TestGroup",
                Targets = new List<AlertTarget>
                {
                    new AlertUrl("http://foo.bar.com"),
                    new AlertEmail("foo@bar.com")
                }
            };
        }

        private void SetupArnForCreateTopic(Mock<IAmazonSimpleNotificationService> sns,
            string topic, string arn)
        {
            var response = new CreateTopicResponse
            {
                TopicArn = arn
            };

            sns
                .Setup(x => x.CreateTopicAsync(topic, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        private void VerifyTopicCreated(Mock<IAmazonSimpleNotificationService> sns)
        {
            sns.Verify(x => x.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyNoTopicCreated(Mock<IAmazonSimpleNotificationService> sns)
        {
            sns.Verify(x => x.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void VerifyHttpSubscriptionAdded(Mock<IAmazonSimpleNotificationService> sns, string arn, string url)
        {
            sns.Verify(x => x.SubscribeAsync(
                It.Is<SubscribeRequest>(r => r.Protocol == "http" && r.TopicArn == arn &&  r.Endpoint == url),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }


        private void VerifyEmailSubscriptionAdded(Mock<IAmazonSimpleNotificationService> sns, string arn, string emailAddress)
        {
            sns.Verify(x => x.SubscribeAsync(
                It.Is<SubscribeRequest>(r => r.Protocol == "email" && r.Endpoint == emailAddress),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }


        private void VerifyNoSubscriptionAdded(Mock<IAmazonSimpleNotificationService> sns)
        {
            sns.Verify(x => x.SubscribeAsync(It.IsAny<SubscribeRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
