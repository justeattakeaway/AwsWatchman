using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration.Generic;
using Watchman.Engine.Generation;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Tests.Generation
{
    [TestFixture]
    public class ResourceNamePopulatorTests
    {
        public class ExampleServiceModel { }

        [Test]
        public async Task PopulateResourceNames_ByPatternAndNamed_ResourcesExpandedWithCorrectThresholds()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(new List<string>
                {
                    "ItemX",
                    "ItemY",
                    "ItemZ"
                });

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var namedItem = new ResourceThresholds<ResourceConfig>
            {
                Name = "ItemX",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(500, 2, null)
                    }
                }
            };

            var patternMatchedItem = new ResourceThresholds<ResourceConfig> {
                Pattern = "Item",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(100, 3, null)
                    }
                }
            };

            var group = new ServiceAlertingGroup<ResourceConfig>
            {
                GroupParameters = new AlertingGroupParameters("name", "suffix"),
                Service = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        namedItem,
                        patternMatchedItem
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            var resources = group.Service.Resources;
            Assert.That(resources.Count, Is.EqualTo(3));

            var itemXThreshold = resources.First(x => x.Name == "ItemX").Values["SomeThreshold"];
            var itemYThreshold = resources.First(x => x.Name == "ItemY").Values["SomeThreshold"];
            var itemZThreshold = resources.First(x => x.Name == "ItemZ").Values["SomeThreshold"];

            Assert.That(itemXThreshold.Threshold, Is.EqualTo(500));
            Assert.That(itemYThreshold.Threshold, Is.EqualTo(100));
            Assert.That(itemZThreshold.Threshold, Is.EqualTo(100));
        }

        [Test]
        public async Task PopulateResourceNames_ByPatternAndNamed_ResourcesExpandedWithCorrectEvaluationPeriods()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(new List<string>
                {
                    "ItemX",
                    "ItemY",
                    "ItemZ"
                });

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var namedItem = new ResourceThresholds<ResourceConfig>
            {
                Name = "ItemX",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(500, 2, null)
                    }
                }
            };

            var patternMatchedItem = new ResourceThresholds<ResourceConfig>
            {
                Pattern = "Item",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(100, 3, null)
                    }
                }
            };

            var group = new ServiceAlertingGroup<ResourceConfig>
            {
                GroupParameters = new AlertingGroupParameters("name", "suffix"),
                Service = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        namedItem,
                        patternMatchedItem
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            var resources = group.Service.Resources;
            Assert.That(resources.Count, Is.EqualTo(3));

            var itemXThreshold = resources.First(x => x.Name == "ItemX").Values["SomeThreshold"];
            var itemYThreshold = resources.First(x => x.Name == "ItemY").Values["SomeThreshold"];
            var itemZThreshold = resources.First(x => x.Name == "ItemZ").Values["SomeThreshold"];

            Assert.That(itemXThreshold.EvaluationPeriods, Is.EqualTo(2));
            Assert.That(itemYThreshold.EvaluationPeriods, Is.EqualTo(3));
            Assert.That(itemZThreshold.EvaluationPeriods, Is.EqualTo(3));
        }

        [Test]
        public async Task PopulateResourceNames_ExclusionPrefixSpecified_ExcludesResources()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(new List<string>
                {
                   "ItemY",
                    "Something"
                });

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig> (new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var group = new ServiceAlertingGroup<ResourceConfig>
            {
                GroupParameters = new AlertingGroupParameters("name", "suffix"),
                Service = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig> { Name = "ItemY" },
                        new ResourceThresholds<ResourceConfig> { Pattern = "Item" },
                        new ResourceThresholds<ResourceConfig> { Name = "Something" }
                    },
                    ExcludeResourcesPrefixedWith = new List<string> { "Item" }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            Assert.That(group.Service.Resources.Count, Is.EqualTo(1));
            Assert.That(group.Service.Resources.First().Name, Is.EqualTo("Something"));
        }

        [Test]
        public async Task PopulateResourceNames_DuplicatesMatched_DoesNotReturnDuplicates()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(new List<string>
                {
                    "ItemY"
                });

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(
                new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var group = new ServiceAlertingGroup<ResourceConfig>
            {
                GroupParameters = new AlertingGroupParameters("name", "suffix"),
                Service = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig> { Name = "ItemY" },
                        new ResourceThresholds<ResourceConfig> { Pattern = "ItemY" },
                        new ResourceThresholds<ResourceConfig> { Pattern = "Item" }
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            Assert.That(group.Service.Resources.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task PopulateResourceNames_NonExistantResourceSpecified_Ingnored()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(new List<string>
                {
                    "ItemY"
                });

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var group = new ServiceAlertingGroup<ResourceConfig>
            {
                GroupParameters = new AlertingGroupParameters("name", "suffix"),
                Service = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig> { Name = "DoesNotExist" }
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            Assert.That(group.Service.Resources.Count, Is.EqualTo(0));
        }
    }
}
