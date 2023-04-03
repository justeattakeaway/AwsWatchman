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
                .Setup(x => x.GetResourcesAsync())
                .ReturnsAsync(WrapResourceNames(new List<string>
                {
                    "ItemX",
                    "ItemY",
                    "ItemZ"
                }));

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var namedItem = new ResourceThresholds<ResourceConfig>
            {
                Name = "ItemX",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(500, 2)
                    }
                }
            };

            var patternMatchedItem = new ResourceThresholds<ResourceConfig>
            {
                Pattern = "Item",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(100, 3)
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
            var result = await sut.PopulateResourceNames(group);

            // assert
            var resources = result.Service.Resources;
            Assert.That(resources.Count, Is.EqualTo(3));

            var itemXThreshold = resources.First(x => x.Definition.Name == "ItemX").Definition.Values["SomeThreshold"];
            var itemYThreshold = resources.First(x => x.Definition.Name == "ItemY").Definition.Values["SomeThreshold"];
            var itemZThreshold = resources.First(x => x.Definition.Name == "ItemZ").Definition.Values["SomeThreshold"];

            Assert.That(itemXThreshold.Threshold, Is.EqualTo(500));
            Assert.That(itemYThreshold.Threshold, Is.EqualTo(100));
            Assert.That(itemZThreshold.Threshold, Is.EqualTo(100));
        }

        private List<AwsResource<ExampleServiceModel>> WrapResourceNames(IEnumerable<string> names)
        {
            return names
                .Select(name =>
                    new AwsResource<ExampleServiceModel>(name, _ => Task.FromResult(new ExampleServiceModel())))
                .ToList();
        }

        [Test]
        public async Task PopulateResourceNames_ByPatternAndNamed_ResourcesExpandedWithCorrectEvaluationPeriods()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourcesAsync())
                .ReturnsAsync(WrapResourceNames(new List<string>
                {
                    "ItemX",
                    "ItemY",
                    "ItemZ"
                }));

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var namedItem = new ResourceThresholds<ResourceConfig>
            {
                Name = "ItemX",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(500, 2)
                    }
                }
            };

            var patternMatchedItem = new ResourceThresholds<ResourceConfig>
            {
                Pattern = "Item",
                Values = new Dictionary<string, AlarmValues>
                {
                    {
                        "SomeThreshold", new AlarmValues(100, 3)
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
            var result = await sut.PopulateResourceNames(group);

            // assert
            var resources = result.Service.Resources;
            Assert.That(resources.Count, Is.EqualTo(3));

            var itemXThreshold = resources.First(x => x.Definition.Name == "ItemX").Definition.Values["SomeThreshold"];
            var itemYThreshold = resources.First(x => x.Definition.Name == "ItemY").Definition.Values["SomeThreshold"];
            var itemZThreshold = resources.First(x => x.Definition.Name == "ItemZ").Definition.Values["SomeThreshold"];

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
                .Setup(x => x.GetResourcesAsync())
                .ReturnsAsync(WrapResourceNames(new List<string>
                {
                    "ItemY",
                    "Something"
                }));

            var sut = new ResourceNamePopulator<ExampleServiceModel, ResourceConfig>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

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
            var result = await sut.PopulateResourceNames(group);

            // assert
            Assert.That(result.Service.Resources.Count, Is.EqualTo(1));
            Assert.That(result.Service.Resources.First().Definition.Name, Is.EqualTo("Something"));
        }

        [Test]
        public async Task PopulateResourceNames_DuplicatesMatched_DoesNotReturnDuplicates()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourcesAsync())
                .ReturnsAsync(WrapResourceNames(new List<string>
                {
                    "ItemY"
                }));

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
            var result = await sut.PopulateResourceNames(group);

            // assert
            Assert.That(result.Service.Resources.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task PopulateResourceNames_NonExistantResourceSpecified_Ignored()
        {
            // arrange
            var resourceSourceStub = new Mock<IResourceSource<ExampleServiceModel>>();
            resourceSourceStub
                .Setup(x => x.GetResourcesAsync())
                .ReturnsAsync(WrapResourceNames(new List<string>
                {
                    "ItemY"
                }));

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
            var result = await sut.PopulateResourceNames(group);

            // assert
            Assert.That(result.Service.Resources.Count, Is.EqualTo(0));
        }
    }
}
