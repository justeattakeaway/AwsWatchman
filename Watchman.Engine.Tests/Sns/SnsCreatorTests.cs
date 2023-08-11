using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using NSubstitute;
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
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertUrl(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task UrlSubscriptionIsAdded()
        {
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
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
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
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
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertEmail(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task ReturnsArnForBothTargets()
        {
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithBoth(), false);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task ReturnsArnInDryRunMode()
        {
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            var arn = await sut.EnsureSnsTopic(ConfigWithAlertUrl(), true);

            Assert.That(arn, Is.Not.Null);
            Assert.That(arn, Is.Not.Empty);
        }

        [Test]
        public async Task NoSubscriptionAddedInDryRunMode()
        {
            var sns = Substitute.For<IAmazonSimpleNotificationService>();
            SetupArnForCreateTopic(sns, "TestGroup-Alerts", "sns-topic-arn");

            var sut = MakeSnsCreator(sns);

            await sut.EnsureSnsTopic(ConfigWithAlertUrl(), true);

            VerifyNoTopicCreated(sns);
            VerifyNoSubscriptionAdded(sns);
        }

        private static SnsCreator MakeSnsCreator(IAmazonSimpleNotificationService sns)
        {
            var logger = new ConsoleAlarmLogger(false);

            return new SnsCreator(
                new SnsTopicCreator(sns, logger),
                new SnsSubscriptionCreator(logger, sns));
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

        private void SetupArnForCreateTopic(IAmazonSimpleNotificationService sns,
            string topic, string arn)
        {
            var response = new CreateTopicResponse
            {
                TopicArn = arn
            };

            sns
                .CreateTopicAsync(topic, Arg.Any<CancellationToken>())
                .Returns(response);
        }

        private void VerifyTopicCreated(IAmazonSimpleNotificationService sns)
        {
            sns.ReceivedWithAnyArgs(1).CreateTopicAsync(default(string));
        }

        private void VerifyNoTopicCreated(IAmazonSimpleNotificationService sns)
        {
            sns.DidNotReceiveWithAnyArgs().CreateTopicAsync(default(string));
        }

        private void VerifyHttpSubscriptionAdded(IAmazonSimpleNotificationService sns, string arn, string url)
        {
            sns.Received(1)
                .SubscribeAsync(
                    Arg.Is<SubscribeRequest>(r => r.Protocol == "http" && r.TopicArn == arn && r.Endpoint == url),
                    Arg.Any<CancellationToken>());
        }


        private void VerifyEmailSubscriptionAdded(IAmazonSimpleNotificationService sns, string arn, string emailAddress)
        {
            sns.Received(1)
                .SubscribeAsync(
                    Arg.Is<SubscribeRequest>(r => r.Protocol == "email" && r.Endpoint == emailAddress),
                    Arg.Any<CancellationToken>());
        }


        private void VerifyNoSubscriptionAdded(IAmazonSimpleNotificationService sns)
        {
            sns.DidNotReceiveWithAnyArgs().SubscribeAsync(default);
        }
    }
}
