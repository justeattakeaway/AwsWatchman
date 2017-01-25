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

            var sut = new ResourceNamePopulator<ExampleServiceModel>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var namedItem = new ResourceThresholds
            {
                Name = "ItemX",
                Thresholds = new Dictionary<string, ThresholdValue>
                {
                    {
                        "SomeThreshold", 500
                    }
                }
            };

            var patternMatchedItem = new ResourceThresholds {
                Pattern = "Item",
                Thresholds = new Dictionary<string, ThresholdValue>
                {
                    {
                        "SomeThreshold", 100
                    }
                }
            };

            var group = new ServiceAlertingGroup
            {
                Service = new AwsServiceAlarms
                {
                    Resources = new List<ResourceThresholds>
                    {
                        namedItem,
                        patternMatchedItem
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            Assert.That(group.Service.Resources.Count, Is.EqualTo(3));
            Assert.That(group.Service.Resources.First(x => x.Name == "ItemX").Thresholds["SomeThreshold"], Is.EqualTo(500));
            Assert.That(group.Service.Resources.First(x => x.Name == "ItemY").Thresholds["SomeThreshold"], Is.EqualTo(100));
            Assert.That(group.Service.Resources.First(x => x.Name == "ItemZ").Thresholds["SomeThreshold"], Is.EqualTo(100));
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

            var sut = new ResourceNamePopulator<ExampleServiceModel>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var group = new ServiceAlertingGroup
            {
                Service = new AwsServiceAlarms
                {
                    Resources = new List<ResourceThresholds>
                    {
                        new ResourceThresholds { Name = "ItemY" },
                        new ResourceThresholds { Pattern = "Item" },
                        new ResourceThresholds { Name = "Something" }
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

            var sut = new ResourceNamePopulator<ExampleServiceModel>(new ConsoleAlarmLogger(false), resourceSourceStub.Object);

            var group = new ServiceAlertingGroup
            {
                Service = new AwsServiceAlarms
                {
                    Resources = new List<ResourceThresholds>
                    {
                        new ResourceThresholds { Name = "ItemY" },
                        new ResourceThresholds { Pattern = "ItemY" },
                        new ResourceThresholds { Pattern = "Item" }
                    }
                }
            };

            // act
            await sut.PopulateResourceNames(group);

            // assert
            Assert.That(group.Service.Resources.Count, Is.EqualTo(1));
        }
    }
}
