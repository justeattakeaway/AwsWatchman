using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdParty.Json.LitJson;
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

        private static Alarm CreateExampleAlarm(List<AlertTarget> targets, AwsResource<FakeResourceType> resource)
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
                AlertingGroup = new ServiceAlertingGroup()
                {
                    Name = "AlertingGroupName",
                    Targets = targets
                },
                Dimensions = new List<Dimension>(),
                Resource = resource
            };
        }


        [Test]
        public void EmailAndUrlTargets_CreatesBothTopics()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", new FakeResourceType());

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(new List<AlertTarget>()
            {
                new AlertEmail() {Email = "test@test.com"},
                new AlertUrl() {Url = "url@url.com"}
            }, resource));

            // act
            var template = new CloudWatchCloudFormationTemplate();
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
            var resource = new AwsResource<FakeResourceType>("name", new FakeResourceType());

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(new List<AlertTarget>(), resource));

            // act
            var template = new CloudWatchCloudFormationTemplate();
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
            var resource = new AwsResource<FakeResourceType>("name", new FakeResourceType());

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(new List<AlertTarget>()
            {
                new AlertEmail() {Email = "test1@test.com"},
                new AlertEmail() {Email = "test2@test.com"},
            }, resource));

            // act
            var template = new CloudWatchCloudFormationTemplate();
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["EmailTopic"];

            Assert.That(emailTopic, Is.Not.Null);

            var emailsInTopic = emailTopic["Properties"]["Subscription"].ToList();

            Assert.That(emailsInTopic.All(j => j["Protocol"].Value<string>() == "email"));
            Assert.That(emailsInTopic.Count, Is.EqualTo(2));

            Assert.That(emailsInTopic.Exists(j => j["Endpoint"].Value<string>() == "test1@test.com"));
            Assert.That(emailsInTopic.Exists(j => j["Endpoint"].Value<string>() == "test2@test.com"));
        }

        [Test]
        public void UrlTargets_CreatedTopicCorrectly()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", new FakeResourceType());

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(new List<AlertTarget>()
            {
                new AlertUrl() {Url = "http://banana"},
                new AlertUrl() {Url = "https://banana2"},
            }, resource));

            // act
            var template = new CloudWatchCloudFormationTemplate();
            template.AddAlarms(alarms);
            var result = template.WriteJson();

            // assert
            var parsed = JObject.Parse(result);
            var emailTopic = parsed["Resources"]["UrlTopic"];

            Assert.That(emailTopic, Is.Not.Null);

            var emailsInTopic = emailTopic["Properties"]["Subscription"].ToList();

            Assert.That(emailsInTopic.Count, Is.EqualTo(2));

            Assert.That(emailsInTopic.Exists(j =>

                 j["Endpoint"].Value<string>() == "http://banana" && j["Protocol"].Value<string>() == "http"
            ));

            Assert.That(emailsInTopic.Exists(j =>

                 j["Endpoint"].Value<string>() == "https://banana2" && j["Protocol"].Value<string>() == "https"
            ));
        }

        [Test]
        public void TargetMappingIsCorrect()
        {
            // arrange
            var resource = new AwsResource<FakeResourceType>("name", new FakeResourceType());

            var alarms = new List<Alarm>();
            alarms.Add(CreateExampleAlarm(new List<AlertTarget>()
            {
                new AlertUrl() {Url = "http://banana"},
                new AlertEmail() {Email = "test@test.com"},
            }, resource));

            // act
            var template = new CloudWatchCloudFormationTemplate();
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
