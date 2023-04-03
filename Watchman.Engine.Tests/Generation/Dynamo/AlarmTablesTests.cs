using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Engine.Generation.Dynamo;

namespace Watchman.Engine.Tests.Generation.Dynamo
{
    [TestFixture]
    public class AlarmTablesTests
    {
        [Test]
        public void SimpleCopyForRead()
        {
            var input = new AlertingGroup
                {
                    AlarmNameSuffix = "fish",
                    DynamoDb = new DynamoDb
                    {
                        Tables = new List<Table> {"table1", "table2"}
                    }
                };

            var alarmTables = AlarmTablesHelper.FilterForRead(input);

            Assert.That(alarmTables, Is.Not.Null);
            Assert.That(alarmTables.AlarmNameSuffix, Is.EqualTo("fish"));
            Assert.That(alarmTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
        }

        [Test]
        public void SimpleCopyForWrite()
        {
            var input = new AlertingGroup
            {
                AlarmNameSuffix = "fish",
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table> { "table1", "table2" }
                }
            };

            var alarmTables = AlarmTablesHelper.FilterForWrite(input);

            Assert.That(alarmTables, Is.Not.Null);
            Assert.That(alarmTables.AlarmNameSuffix, Is.EqualTo("fish"));
            Assert.That(alarmTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
        }

        [Test]
        public void FilterIsAppliedForReadAndWrite()
        {
            var input = new AlertingGroup
            {
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table> { "table1", "table2", "not_this_one" },
                    ExcludeTablesPrefixedWith = new List<string> { "not" }
                }
            };

            var readTables = AlarmTablesHelper.FilterForRead(input);
            var writeTables = AlarmTablesHelper.FilterForRead(input);

            Assert.That(readTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
            Assert.That(writeTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
        }

        [Test]
        public void ReadFilterIsAppliedForReadOnly()
        {
            var input = new AlertingGroup
            {
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table> { "table1", "table2", "not_this_one" },
                    ExcludeTablesPrefixedWith = new List<string> { "not" },
                    ExcludeReadsForTablesPrefixedWith = new List<string> { "table1" },
                    ExcludeWritesForTablesPrefixedWith = new List<string> { "nomatch" }
                }
            };

            var readTables = AlarmTablesHelper.FilterForRead(input);
            var writeTables = AlarmTablesHelper.FilterForWrite(input);

            Assert.That(readTables.Tables, Is.EquivalentTo(new List<Table> { "table2" }));
            Assert.That(writeTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
        }

        [Test]
        public void WriteFilterIsAppliedForWriteOnly()
        {
            var input = new AlertingGroup
            {
                DynamoDb = new DynamoDb
                {
                    Tables = new List<Table> { "table1", "table2", "not_this_one" },
                    ExcludeTablesPrefixedWith = new List<string> { "not" },
                    ExcludeReadsForTablesPrefixedWith = new List<string> { "nomatch" },
                    ExcludeWritesForTablesPrefixedWith = new List<string> { "table1" }
                }
            };

            var readTables = AlarmTablesHelper.FilterForRead(input);
            var writeTables = AlarmTablesHelper.FilterForWrite(input);

            Assert.That(readTables.Tables, Is.EquivalentTo(new List<Table> { "table1", "table2" }));
            Assert.That(writeTables.Tables, Is.EquivalentTo(new List<Table> { "table2" }));
        }
    }
}
