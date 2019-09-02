using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Tests.Generation.Generic
{
    class FakeResourceType { }

    [TestFixture]
    public class CloudWatchCloudFormationTemplateTests
    {
        private const string AlertingGroupName = "AlertingGroupName";

        private static Alarm CreateExampleAlarm(AwsResource<FakeResourceType> resource)
        {
            return new Alarm()
            {
                AlarmDefinition = new AlarmDefinition()
                {
                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                    Statistic = Statistic.Average,
                    Threshold = new Threshold() {ThresholdType = ThresholdType.Absolute},
                    AlertOnInsufficientData = true,
                    AlertOnOk = true
                },
                AlarmName = "",
                Dimensions = new List<Dimension>(),
                ResourceIdentifier = resource.Name
            };
        }


        [Test]
        public void EmailAndUrlTargets_CreatesBothTopics()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", _ => Task.FromResult(new FakeResourceType()));

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(resource));

            var targets = new List<AlertTarget>()
            {
                new AlertEmail("test@test.com"),
                new AlertUrl("url@url.com")
            };

            // act
            var template = new CloudWatchCloudFormationTemplate("group-name", targets);
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["EmailTopic"];
            var urlTopic = parsed["Resources"]["UrlTopic"];

            Assert.That(emailTopic, Is.Not.Null);
            Assert.That(urlTopic, Is.Not.Null);
        }

        [Test]
        public void NoTargets_CreatesNoTopics()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", _ => Task.FromResult(new FakeResourceType()));

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(resource));

            // act
            var template = new CloudWatchCloudFormationTemplate("group-name", new List<AlertTarget>());
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["EmailTopic"];
            var urlTopic = parsed["Resources"]["UrlTopic"];

            Assert.That(emailTopic, Is.Null);
            Assert.That(urlTopic, Is.Null);
        }

        [Test]
        public void EmailTargets_CreatedTopicCorrectly()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", _ => Task.FromResult(new FakeResourceType()));
            var targets = new List<AlertTarget>()
            {
                new AlertEmail("test1@test.com"),
                new AlertEmail("test2@test.com")
            };
            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(resource));

            // act
            var template = new CloudWatchCloudFormationTemplate(AlertingGroupName, targets);
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["EmailTopic"];

            Assert.That(emailTopic, Is.Not.Null);

            var emailsInTopic = emailTopic["Properties"]["Subscription"].ToList();

            Assert.That(emailsInTopic.All(j => (string) j["Protocol"] == "email"));
            Assert.That(emailsInTopic.Count, Is.EqualTo(2));

            Assert.That(emailsInTopic.Exists(j => (string) j["Endpoint"] == "test1@test.com"));
            Assert.That(emailsInTopic.Exists(j => (string) j["Endpoint"] == "test2@test.com"));
        }

        [Test]
        public void UrlTargets_CreatedTopicCorrectly()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", _ => Task.FromResult(new FakeResourceType()));

            var alarms = new List<Alarm>();
            var targets = new List<AlertTarget>()
            {
                new AlertUrl("http://banana"),
                new AlertUrl("https://banana2"),
            };
            alarms.Add(CreateExampleAlarm(resource));

            // act
            var template = new CloudWatchCloudFormationTemplate(AlertingGroupName, targets);
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["UrlTopic"];

            Assert.That(emailTopic, Is.Not.Null);

            var emailsInTopic = emailTopic["Properties"]["Subscription"].ToList();

            Assert.That(emailsInTopic.Count, Is.EqualTo(2));

            Assert.That(emailsInTopic.Exists(j =>

                 (string)j["Endpoint"] == "http://banana" && (string)j["Protocol"] == "http"
            ));

            Assert.That(emailsInTopic.Exists(j =>

                 (string)j["Endpoint"] == "https://banana2" && (string)j["Protocol"] == "https"
            ));
        }

        [Test]
        public void TargetMappingIsCorrect()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", _ => Task.FromResult(new FakeResourceType()));

            var alarms = new List<Alarm>();
            var targets = new List<AlertTarget>()
            {
                new AlertUrl("http://banana"),
                new AlertEmail("test@test.com"),
            };
            alarms.Add(CreateExampleAlarm(resource));

            // act
            var template = new CloudWatchCloudFormationTemplate("group-name", targets);
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);

            var alarm = ((JObject)parsed["Resources"])
                .Properties()
                .Single(j =>j.Value["Type"].Value<string>() == "AWS::CloudWatch::Alarm")
                .Value;

            var okTargets = (JArray)alarm["Properties"]["OKActions"];
            var insufficientTargets = (JArray)alarm["Properties"]["InsufficientDataActions"];
            var alarmTargets = (JArray)alarm["Properties"]["AlarmActions"];

            // ok alarms only go to urls, everything else goes to email also
            Assert.That((string)okTargets.Single()["Ref"], Is.EqualTo("UrlTopic"));

            Assert.That(insufficientTargets.Any(j => (string) j["Ref"] == "UrlTopic"));
            Assert.That(insufficientTargets.Any(j => (string)j["Ref"] == "EmailTopic"));
            Assert.That(alarmTargets.Any(j => (string)j["Ref"] == "UrlTopic"));
            Assert.That(alarmTargets.Any(j => (string)j["Ref"] == "EmailTopic"));
        }
    }
}
