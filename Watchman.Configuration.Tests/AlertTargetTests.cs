using NUnit.Framework;

namespace Watchman.Configuration.Tests
{
    [TestFixture]
    public class AlertTargetTests
    {
        [Test]
        public void EqualsIsFalseForNullEmail()
        {
            var instance = new AlertEmail("a@b.com");

            Assert.That(instance, Is.Not.EqualTo(null));
        }

        [Test]
        public void EqualsIsFalseForNullUrl()
        {
            var instance = new AlertUrl("http://a.com/b");

            Assert.That(instance, Is.Not.EqualTo(null));
        }

        [Test]
        public void SelfEqualIsTrue()
        {
            var instance1 = new AlertUrl("http://a.com/b");
            var instance2 = new AlertEmail("abc");

            Assert.That(instance1, Is.EqualTo(instance1));
            Assert.That(instance2, Is.EqualTo(instance2));

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }

        [Test]
        public void DifferentEmailAndUrlAreNotEqual()
        {
            var instance1 = new AlertUrl("a@b.c");
            var instance2 = new AlertEmail("http://abc");

            AssertInequalities(instance1, instance2);
        }

        [Test]
        public void SameEmailAndUrlAreNotEqual()
        {
            var instance1 = new AlertUrl("a.b.c");
            var instance2 = new AlertEmail("a.b.c");

            // Can't call AssertInequalities because the hashcodes are same for same strings
            // This is ot a problem in practice
            // as emails and urls have different formats

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }

        [Test]
        public void EqualsIsTrueForEquivalentEmails()
        {
            var instance1 = new AlertEmail("a@b.com");
            var instance2 = new AlertEmail("a@b.com");

            AssertEqualities(instance1, instance2);
        }

        [Test]
        public void EqualsIsFalseForDifferentEmails()
        {
            var instance1 = new AlertEmail("a@b.com");
            var instance2 = new AlertEmail("beee@ceee.org");

            AssertInequalities(instance1, instance2);
        }

        [Test]
        public void EqualsIsTrueForEquivalentUrls()
        {
            var instance1 = new AlertUrl("http://a.com/b");
            var instance2 = new AlertUrl("http://a.com/b");

            AssertEqualities(instance1, instance2);
        }

        [Test]
        public void EqualsIsFalseForDifferentUrls()
        {
            var instance1 = new AlertUrl("http://a.com/b");
            var instance2 = new AlertUrl("https://beee.org/ceee");

            AssertInequalities(instance1, instance2);
        }

        private void AssertEqualities(AlertTarget a, AlertTarget b)
        {
            Assert.That(a, Is.EqualTo(a));
            Assert.That(b, Is.EqualTo(b));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(b, Is.EqualTo(a));

            Assert.That(a, Is.Not.EqualTo(null));
            Assert.That(b, Is.Not.EqualTo(null));

            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        private void AssertInequalities(AlertTarget a, AlertTarget b)
        {
            Assert.That(a, Is.EqualTo(a));
            Assert.That(b, Is.EqualTo(b));

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(b, Is.Not.EqualTo(a));

            Assert.That(a, Is.Not.EqualTo(null));
            Assert.That(b, Is.Not.EqualTo(null));

            Assert.That(a.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }
    }
}
