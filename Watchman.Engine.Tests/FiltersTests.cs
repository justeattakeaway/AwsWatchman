using NUnit.Framework;
using Watchman.Configuration;
using Watchman.Engine.Generation;

namespace Watchman.Engine.Tests
{
    [TestFixture]
    public class FiltersTests
    {
        [Test]
        public void EmptyListIsNotFiltered()
        {
            var prefixes = new List<string> {"ab", "xy "};

            var result = new List<Table>()
                .ExcludePrefixes(prefixes, t => t.Name)
                .ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void NoFilteringWhenNoMatches()
        {
            var source = new List<Table> {"c_entry1", "b_entry2" };
            var prefixes = new List<string> { "ab", "xy " };

            var result = source.ExcludePrefixes(prefixes, t => t.Name)
                .ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Is.EquivalentTo(source));
        }

        [Test]
        public void NoFilteringWhenNoPrefixes()
        {
            var source = new List<Table> { "c_entry1", "b_entry2" };

            var result = source
                .ExcludePrefixes(new List<string>(), t => t.Name)
                .ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Is.EquivalentTo(source));
        }

        [Test]
        public void WhenAllMatch_AllAreFiltered()
        {
            var source = new List<Table> { "ab_entry1", "xy_entry2" };
            var prefixes = new List<string> { "ab", "xy" };

            var result = source.ExcludePrefixes(prefixes, t => t.Name)
                .ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void WhenSomeMatch_MatchesAreFiltered()
        {
            var source = new List<Table> { "ab_entry1", "cd_entry2" };
            var prefixes = new List<string> { "ab", "xy" };

            var result = source.ExcludePrefixes(prefixes, t => t.Name)
                .ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result, Is.EquivalentTo(new List<Table> { "cd_entry2" }));
        }

        [Test]
        public void MultipleEntriesMultipleMatches()
        {
            var data = new List<Table>
            {
                new Table { Name = "aaTable" },
                new Table { Name = "bbTable" },
                new Table { Name = "ccTable" },
                new Table { Name = "ddTable" }
            };

            var tablePrefixes = new List<string> { "aa", "cc" };

            var filtered = data
                .ExcludePrefixes(tablePrefixes, ti => ti.Name)
                .ToList();

            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered.Count(ids => ids.Name == "aaTable"), Is.EqualTo(0));
            Assert.That(filtered.Count(ids => ids.Name == "bbTable"), Is.EqualTo(1));
            Assert.That(filtered.Count(ids => ids.Name == "ccTable"), Is.EqualTo(0));
            Assert.That(filtered.Count(ids => ids.Name == "ddTable"), Is.EqualTo(1));
        }
    }
}
