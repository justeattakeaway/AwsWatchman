using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Tests.Generation
{
    [TestFixture]
    public class ServiceAlarmBuilderTests
    {
        public class FakeResource { }

        private Mock<IResourceSource<FakeResource>> _fakeTableSource;
        private Mock<IAlarmDimensionProvider<FakeResource>> _fakeDimensionProvider;
        private Mock<IResourceAttributesProvider<FakeResource>> _fakeAttributeProvider;

        private ServiceAlarmBuilder<FakeResource> _generator;

        [SetUp]
        public void SetUp()
        {
            _fakeTableSource = new Mock<IResourceSource<FakeResource>>();
            _fakeDimensionProvider = new Mock<IAlarmDimensionProvider<FakeResource>>();
            _fakeAttributeProvider = new Mock<IResourceAttributesProvider<FakeResource>>();

            _generator = new ServiceAlarmBuilder<FakeResource>(
                _fakeTableSource.Object,
                _fakeDimensionProvider.Object,
                _fakeAttributeProvider.Object);
        }

        private void SetupFakeResources(IList<string> resourceNames)
        {
            _fakeTableSource.Setup(x => x.GetResourceNamesAsync())
                   .ReturnsAsync(resourceNames);

            foreach (var resource in resourceNames)
            {
                _fakeTableSource.Setup(x => x.GetResourceAsync(resource))
                    .ReturnsAsync(new AwsResource<FakeResource>(resource, new FakeResource()));
            }
        }

        [Test]
        public async Task ResourceThresholdsTakePrecedenceOverDefaults()
        {
            // arrange

            var defaults = new List<AlarmDefinition>()
            {
                new AlarmDefinition()
                {
                    Name = "AlarmName",
                    Threshold = new Threshold()
                    {
                        ThresholdType = ThresholdType.Absolute,
                        Value = 400
                    }
                }
            };

            var alertingGroup = new ServiceAlertingGroup()
            {
                AlarmNameSuffix = "Suffix",
                Name = "TestAlarm",
                Service = new AwsServiceAlarms()
                {
                    Resources = new List<ResourceThresholds>()
                    {
                        new ResourceThresholds()
                        {
                            Name = "ResourceA",
                            Thresholds = new Dictionary<string, double>()
                            {
                                {"AlarmName", 200}
                            }
                        },
                         new ResourceThresholds()
                        {
                            Name = "ResourceB"
                        }
                    }
                }
            };

            SetupFakeResources(new[] {"ResourceA", "ResourceB"});

            // act

            var result = await _generator.GenerateAlarmsFor(alertingGroup, "sns-topic-arn", defaults);

            // assert

            var resourceAlarmA = result.FirstOrDefault(x => x.Resource.Name == "ResourceA");
            var resourceAlarmB = result.FirstOrDefault(x => x.Resource.Name == "ResourceB");

            Assert.That(resourceAlarmA, Is.Not.Null);
            Assert.That(resourceAlarmA.AlarmDefinition.Threshold.Value, Is.EqualTo(200));
            Assert.That(resourceAlarmB, Is.Not.Null);
            Assert.That(resourceAlarmB.AlarmDefinition.Threshold.Value, Is.EqualTo(400));
        }

        [Test]
        public async Task ResourceAndGroupThresholdsTakePrecedenceOverDefault()
        {
            // arrange

            var defaults = new List<AlarmDefinition>()
            {
                new AlarmDefinition()
                {
                    Name = "AlarmName",
                    Threshold = new Threshold()
                    {
                        ThresholdType = ThresholdType.Absolute,
                        Value = 400
                    }
                }
            };

            var alertingGroup = new ServiceAlertingGroup()
            {
                AlarmNameSuffix = "Suffix",
                Name = "TestAlarm",
                Service = new AwsServiceAlarms()
                {
                    Resources = new List<ResourceThresholds>()
                    {
                        new ResourceThresholds()
                        {
                            Name = "ResourceA",
                            Thresholds = new Dictionary<string, double>()
                            {
                                {"AlarmName", 200}
                            }
                        },
                         new ResourceThresholds()
                        {
                            Name = "ResourceB"
                        }
                    },
                    Thresholds = new Dictionary<string, double>()
                    {
                        { "AlarmName", 300 }
                    }
                }
            };

            SetupFakeResources(new[] { "ResourceA", "ResourceB" });

            // act

            var result = await _generator.GenerateAlarmsFor(alertingGroup, "sns-topic-arn", defaults);

            // assert

            var resourceAlarmA = result.FirstOrDefault(x => x.Resource.Name == "ResourceA");
            var resourceAlarmB = result.FirstOrDefault(x => x.Resource.Name == "ResourceB");

            Assert.That(resourceAlarmA, Is.Not.Null);
            Assert.That(resourceAlarmA.AlarmDefinition.Threshold.Value, Is.EqualTo(200));
            Assert.That(resourceAlarmB, Is.Not.Null);
            Assert.That(resourceAlarmB.AlarmDefinition.Threshold.Value, Is.EqualTo(300));
        }
    }
}
