using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.Configuration;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Tests.Generation.Dynamo
{
    [TestFixture]
    public class OrphanTablesReporterTests
    {
        [Test]
        public async Task WithNoDataThereAreNoOrphans()
        {
            var loader = MockTableSource(new List<string>());

            var reporter = new OrphanTablesFinder(loader.Object);

            var config = MakeConfigFor(new AlertingGroup());

           var orphans = await reporter.FindOrphanTables(config);

            AssertNoOrphans(orphans);
        }

        [Test]
        public async Task WhenNoTablesAreMonitoredAllAreOrphans()
        {
            var loader = MockTableSource(new List<string> { "tableA", "tableB", "tableC" });

            var reporter = new OrphanTablesFinder(loader.Object);

            var config = MakeConfigFor(new AlertingGroup());

            var orphans = await reporter.FindOrphanTables(config);

            AssertHasOrphans(orphans, new List<string> { "tableA", "tableB", "tableC" });
        }

        [Test]
        public async Task WhenATablesIsMonitoreditIsNotAnOrphan()
        {
            var loader = MockTableSource(new List<string> { "tableA", "tableB", "tableC" });

            var reporter = new OrphanTablesFinder(loader.Object);

            var config = MakeConfigFor(new AlertingGroup
                {
                    DynamoDb = new DynamoDb
                    {
                        Tables = new List<Table> { "tableA" }
                    }
                });

            var orphans = await reporter.FindOrphanTables(config);

            AssertHasOrphans(orphans, new List<string> { "tableB", "tableC" });
        }

        [Test]
        public async Task MultipleAlertingGroupsAreCovered()
        {
            var loader = MockTableSource(new List<string> { "tableA", "tableB", "tableC", "tableD" });

            var reporter = new OrphanTablesFinder(loader.Object);

            var config =  new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup>
                {
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableA" }
                            }
                        },
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableD" }
                            }
                        }
                }
            };

            var orphans = await reporter.FindOrphanTables(config);

            AssertHasOrphans(orphans, new List<string> { "tableB", "tableC" });
        }

        [Test]
        public async Task AllTablesCanBeCovered()
        {
            var loader = MockTableSource(new List<string> { "tableA", "tableB", "tableC", "tableD" });

            var reporter = new OrphanTablesFinder(loader.Object);

            var config = new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup>
                {
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableA", "tableC" }
                            }
                        },
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableD", "tableB" }
                            }
                        }
                }
            };

            var orphans = await reporter.FindOrphanTables(config);

            AssertNoOrphans(orphans);
        }

        [Test]
        public async Task CatchAllgroupCanBeExcluded()
        {
            var loader = MockTableSource(new List<string> { "tableA", "tableB", "tableC", "tableD" });

            var reporter = new OrphanTablesFinder(loader.Object);

            var config = new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup>
                {
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableA" }
                            }
                        },
                    new AlertingGroup
                        {
                            DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableD" }
                            }
                        },
                   new AlertingGroup
                       {
                        Name = "catchAll",
                        IsCatchAll = true,
                        DynamoDb = new DynamoDb
                            {
                                Tables = new List<Table> { "tableA", "tableB", "tableC", "tableD" }
                            }
                       }
                }
            };

            var orphans = await reporter.FindOrphanTables(config);

            AssertHasOrphans(orphans, new List<string> { "tableB", "tableC" });
        }

        private static Mock<IResourceSource<TableDescription>> MockTableSource(List<string> tableNames)
        {
            var loader = new Mock<IResourceSource<TableDescription>>();
            loader.Setup(l => l.GetResourceNamesAsync())
                .ReturnsAsync(tableNames);

            return loader;
        }

        private WatchmanConfiguration MakeConfigFor(AlertingGroup group)
        {
            return new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup> { group }
            };
        }

        private void AssertHasOrphans(OrphansModel orphans, IEnumerable<string> items)
        {
            Assert.That(orphans, Is.Not.Null);
            Assert.That(orphans.Items, Is.Not.Null);

            Assert.That(orphans.Items, Is.Not.Empty);
            Assert.That(orphans.Items, Is.EquivalentTo(items));
        }

        private void AssertNoOrphans(OrphansModel orphans)
        {
            Assert.That(orphans, Is.Not.Null);
            Assert.That(orphans.Items, Is.Not.Null);
            Assert.That(orphans.Items, Is.Empty);
        }

    }
}
