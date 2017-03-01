using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.Configuration;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Tests.Generation.Dynamo
{
    [TestFixture]
    public class ConfigPopulatorTests
    {
        private Mock<IResourceSource<TableDescription>> _tableLoaderMock;

        [Test]
        public async Task ASingleTableIsPassedThrough()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Name = "tableA", Threshold = 0.5 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(1));
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "tableA");

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Never);
        }

        [Test]
        public async Task TableListIsPassedThrough()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Name = "tableA", Threshold = 0.5},
                        new Table { Name = "tableB", Threshold = 0.4}
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(2));
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "tableA");
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "tableB");

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Never);
        }

        [Test]
        public async Task MatchAllTablesGetsTablesFromTableLoader()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = ".*" }
                    }
                }
            };

            var populator = CreatePopulator(true);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(3));
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "AutoTable1");
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "AutoTable2");
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "ATable3");

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task TablePatternIsExpandedWithDefaultData()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "^Auto" }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(2));
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "AutoTable1");
            ShouldHaveTable(alertingGroup.DynamoDb.Tables, "AutoTable2");

            var autoTable1 = alertingGroup.DynamoDb.Tables[0];
            Assert.That(autoTable1.Name, Is.EqualTo("AutoTable1"));
            Assert.That(autoTable1.Threshold, Is.Null);
            Assert.That(autoTable1.MonitorWrites, Is.Null);

            var autoTable2 = alertingGroup.DynamoDb.Tables[1];
            Assert.That(autoTable2.Name, Is.EqualTo("AutoTable2"));
            Assert.That(autoTable2.Threshold, Is.Null);
            Assert.That(autoTable2.MonitorWrites, Is.Null);

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task TablePatternDataIsKeptOnExpandedRows()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "^Auto", Threshold = 0.42, MonitorWrites = false }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(2));

            var autoTable1 = alertingGroup.DynamoDb.Tables[0];
            Assert.That(autoTable1.Name, Is.EqualTo("AutoTable1"));
            Assert.That(autoTable1.Threshold, Is.EqualTo(0.42));
            Assert.That(autoTable1.MonitorWrites, Is.False);

            var autoTable2 = alertingGroup.DynamoDb.Tables[1];
            Assert.That(autoTable2.Name, Is.EqualTo("AutoTable2"));
            Assert.That(autoTable2.Threshold, Is.EqualTo(0.42));
            Assert.That(autoTable2.MonitorWrites, Is.False);

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task TablePatternThrottlingDataIsKeptOnExpandedRows()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "^Auto", MonitorThrottling = true, ThrottlingThreshold = 123 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(2));

            var autoTable1 = alertingGroup.DynamoDb.Tables[0];
            Assert.That(autoTable1.Name, Is.EqualTo("AutoTable1"));
            Assert.That(autoTable1.MonitorThrottling, Is.True);
            Assert.That(autoTable1.ThrottlingThreshold, Is.EqualTo(123));

            var autoTable2 = alertingGroup.DynamoDb.Tables[1];
            Assert.That(autoTable2.Name, Is.EqualTo("AutoTable2"));
            Assert.That(autoTable2.MonitorThrottling, Is.True);
            Assert.That(autoTable2.ThrottlingThreshold, Is.EqualTo(123));

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task TablePatternCanCoExistWithTableName()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "^Auto", Threshold = 0.42 },
                        new Table { Name = "Foo", Threshold = 0.53 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(3));

            var fooTable = alertingGroup.DynamoDb.Tables[0];
            Assert.That(fooTable.Name, Is.EqualTo("Foo"));
            Assert.That(fooTable.Threshold, Is.EqualTo(0.53));

            var autoTable1 = alertingGroup.DynamoDb.Tables[1];
            Assert.That(autoTable1.Name, Is.EqualTo("AutoTable1"));
            Assert.That(autoTable1.Threshold, Is.EqualTo(0.42));

            var autoTable2 = alertingGroup.DynamoDb.Tables[2];
            Assert.That(autoTable2.Name, Is.EqualTo("AutoTable2"));
            Assert.That(autoTable2.Threshold, Is.EqualTo(0.42));

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task CanHaveMoreThanOnePatternWithDifferentData()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "Table1", Threshold = 0.42, MonitorWrites = true },
                        new Table { Pattern = "Table2", Threshold = 0.53, MonitorWrites = false }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(2));

            var autoTable1 = alertingGroup.DynamoDb.Tables[0];
            Assert.That(autoTable1.Name, Is.EqualTo("AutoTable1"));
            Assert.That(autoTable1.Threshold, Is.EqualTo(0.42));
            Assert.That(autoTable1.MonitorWrites, Is.True);

            var autoTable2 = alertingGroup.DynamoDb.Tables[1];
            Assert.That(autoTable2.Name, Is.EqualTo("AutoTable2"));
            Assert.That(autoTable2.Threshold, Is.EqualTo(0.53));
            Assert.That(autoTable2.MonitorWrites, Is.False);

            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.AtLeastOnce);
        }

        [Test]
        public async Task CanHaveCatchAllPattern()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "(.*)", Threshold = 0.42 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            Assert.That(alertingGroup.DynamoDb.Tables.Count, Is.EqualTo(9));
            _tableLoaderMock.Verify(t => t.GetResourceNamesAsync(), Times.Once);
        }

        [Test]
        public async Task PatternAndTableNameOverlapDoesNotDuplicateAlerts()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "order", Threshold = 0.42 },
                        new Table { Name = "orders_dispatched", Threshold = 0.62 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            var tables = alertingGroup.DynamoDb.Tables;

            Assert.That(tables.Count, Is.EqualTo(3));
            ShouldHaveTable(tables, "orders_dispatched");
            ShouldHaveTable(tables, "order_data");
            ShouldHaveTable(tables, "other_orders");

            Assert.That(tables
                .First(t => t.Name == "orders_dispatched")
               .Threshold, Is.EqualTo(0.62));

            Assert.That(tables
                .First(t => t.Name == "order_data")
                .Threshold, Is.EqualTo(0.42));

            Assert.That(tables
                .First(t => t.Name == "other_orders")
                .Threshold, Is.EqualTo(0.42));
        }

        [Test]
        public async Task TwoPatternsOverlapDoesNotDuplicateAlerts()
        {
            var alertingGroup = new AlertingGroup
            {
                Name = "test",
                AlarmNameSuffix = "test",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table>
                    {
                        new Table { Pattern = "orders_dispatched", Threshold = 0.62 },
                        new Table { Pattern = "order", Threshold = 0.42 }
                    }
                }
            };

            var populator = CreatePopulator(false);

            await populator.PopulateDynamoTableNames(alertingGroup);

            var tables = alertingGroup.DynamoDb.Tables;

            Assert.That(tables.Count, Is.EqualTo(3));

            ShouldHaveTable(tables, "orders_dispatched");
            ShouldHaveTable(tables, "order_data");
            ShouldHaveTable(tables, "other_orders");

            Assert.That(tables
                .First(t => t.Name == "orders_dispatched")
                .Threshold, Is.EqualTo(0.62));

            Assert.That(tables
                .First(t => t.Name == "order_data")
                .Threshold, Is.EqualTo(0.42));

            Assert.That(tables
                .First(t => t.Name == "other_orders")
                .Threshold, Is.EqualTo(0.42));
        }

        private TableNamePopulator CreatePopulator(bool shortList)
        {
            var tableNames = new List<string>
                {
                    "AutoTable1",
                    "AutoTable2",
                    "ATable3"
                };

            if (!shortList)
            {
                tableNames.Add("order_data");
                tableNames.Add("orders_dispatched");
                tableNames.Add("other_orders");
                tableNames.Add("fish");
                tableNames.Add("foo");
                tableNames.Add("woof");
            }

            var logger = new Mock<IAlarmLogger>();
            _tableLoaderMock = new Mock<IResourceSource<TableDescription>>();

            _tableLoaderMock.Setup(t => t.GetResourceNamesAsync())
                .ReturnsAsync(tableNames);

            return new TableNamePopulator(logger.Object, _tableLoaderMock.Object);
        }

        private void ShouldHaveTable(IEnumerable<Table> tables, string name)
        {
            var matchCount = tables.Count(t => t.Name == name);
            Assert.That(matchCount, Is.EqualTo(1), "Wrong count for table " + name);
        }
    }
}
