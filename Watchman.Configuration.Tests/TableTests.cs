using NUnit.Framework;

namespace Watchman.Configuration.Tests
{
    [TestFixture]
    public class TableTests
    {
        [Test]
        public void EmptyObjectsAreEqual()
        {
            var table1 = new Table();
            var table2 = new Table();

            Assert.That(table1.Equals(table2));
        }

        [Test]
        public void PopulatedObjectsAreEqual()
        {
            var table1 = new Table {Name = "name", Threshold = 0.4};
            var table2 = new Table { Name = "name", Threshold = 0.4 };

            Assert.That(table1.Equals(table2));
        }

        [Test]
        public void DifferentObjectsAreNotEqual()
        {
            var table1 = new Table { Name = "name1" };
            var table2 = new Table { Name = "name2" };

            Assert.That(table1.Equals(table2), Is.False);
        }

        [Test]
        public void SamePatternsAreEqual()
        {
            var table1 = new Table { Pattern = "name1",};
            var table2 = new Table { Pattern = "name1" };

            Assert.That(table1.Equals(table2));
        }

        [Test]
        public void DifferentPatternsAreNotEqual()
        {
            var table1 = new Table { Pattern = "name1" };
            var table2 = new Table { Pattern = "name2" };

            Assert.That(table1.Equals(table2), Is.False);
        }

        [Test]
        public void PatternDoesNotMatchName()
        {
            var table1 = new Table { Name = "name1" };
            var table2 = new Table { Pattern = "name1" };

            Assert.That(table1.Equals(table2), Is.False);
        }

        [Test]
        public void WhenMonitorWritesDoesNotMatch()
        {
            var table1 = new Table { Name = "name1", MonitorWrites = true };
            var table2 = new Table { Name = "name1", MonitorWrites = false };

            Assert.That(table1.Equals(table2), Is.False);
        }

        [Test]
        public void HasHashCodes()
        {
            var table1 = new Table { Name = "name1" };
            var table2 = new Table { Pattern = "name2" };
            var table3 = new Table { Name = "name", Threshold = 0.54 };
            var table4 = new Table { Pattern = "name", Threshold = 0.54 };

            var hash1 = table1.GetHashCode();
            var hash2 = table2.GetHashCode();
            var hash3 = table3.GetHashCode();
            var hash4 = table4.GetHashCode();

            Assert.That(hash1, Is.Not.EqualTo(hash2));
            Assert.That(hash1, Is.Not.EqualTo(hash3));
            Assert.That(hash1, Is.Not.EqualTo(hash4));

            Assert.That(hash2, Is.Not.EqualTo(hash3));
            Assert.That(hash2, Is.Not.EqualTo(hash4));

            Assert.That(hash3, Is.Not.EqualTo(hash4));
        }
    }
}
