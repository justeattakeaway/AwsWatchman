using NUnit.Framework;
using Watchman.Engine.Generation;

namespace Watchman.Engine.Tests
{
    [TestFixture]
    public class RegexFiltersTests
    {
        [Test]
        public void TestWhereSimpleRegexIsMatch()
        {
            var filtered = MakeTestData()
                .WhereRegexIsMatch("fish")
                .ToList();

            Assert.That(filtered.Count, Is.EqualTo(3));
            Assert.That(filtered.All(t => t.ToLowerInvariant().Contains("fish")), Is.True);
        }

        [Test]
        public void TestWhereStartAnchoredRegexIsMatch()
        {
            var filtered = MakeTestData()
                .WhereRegexIsMatch("^fish")
                .ToList();

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered.All(t => t.ToLowerInvariant().StartsWith("fish")), Is.True);
        }

        [Test]
        public void TestWhereEndAnchoredRegexIsMatch()
        {
            var filtered = MakeTestData()
                .WhereRegexIsMatch("fish$")
                .ToList();

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered.All(t => t.ToLowerInvariant().EndsWith("fish")), Is.True);
        }

        [Test]
        public void TestWhereRegexMatchesAllTables()
        {
            var data = MakeTestData();

            var filtered = data
                .WhereRegexIsMatch(".*")
                .ToList();

            Assert.That(filtered.Count, Is.EqualTo(data.Count));
        }

        private static List<string> MakeTestData()
        {
            return new List<string>
            {
                "aaTable",
                "FISHTable",
                "bbFishTable",
                "ccTable_fish",
                "ddTable",
                "nomatch",
                "match-ish"
            };
        }
    }
}
