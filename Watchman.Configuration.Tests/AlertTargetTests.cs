using NUnit.Framework;

namespace Watchman.Configuration.Tests
{
    [TestFixture]
    public class AlertTargetTests
    {
        [Test]
        public void EqualsIsFalseForNullEmail()
        {
            var instance = new AlertEmail { Email = "a@b.com" };

            Assert.That(instance, Is.Not.EqualTo(null));
        }

        [Test]
        public void EqualsIsFalseForNullUrl()
        {
            var instance = new AlertUrl { Url = "http://a.com/b" };

            Assert.That(instance, Is.Not.EqualTo(null));
        }

        [Test]
        public void SelfEqualIsTrue()
        {
            var instance1 = new AlertUrl { Url = "http://a.com/b" };
            var instance2 = new AlertEmail { Email = "abc" };

            Assert.That(instance1, Is.EqualTo(instance1));
            Assert.That(instance2, Is.EqualTo(instance2));

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }

        [Test]
        public void EmailIsNotEqualToUrl()
        {
            var instance1 = new AlertUrl { Url = "abc" };
            var instance2 = new AlertEmail { Email = "abc" };

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }

        [Test]
        public void EqualsIsTrueForEquivalentEmails()
        {
            var instance1 = new AlertEmail { Email = "a@b.com" };
            var instance2 = new AlertEmail { Email = "a@b.com" };

            Assert.That(instance1, Is.EqualTo(instance2));
            Assert.That(instance2, Is.EqualTo(instance1));
        }

        [Test]
        public void EqualsIsFalseForDifferentEmails()
        {
            var instance1 = new AlertEmail { Email = "a@b.com" };
            var instance2 = new AlertEmail { Email = "beee@ceee.org" };

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }

        [Test]
        public void EqualsIsTrueForEquivalentUrls()
        {
            var instance1 = new AlertUrl { Url = "http://a.com/b" };
            var instance2 = new AlertUrl { Url = "http://a.com/b" };

            Assert.That(instance1, Is.EqualTo(instance2));
            Assert.That(instance2, Is.EqualTo(instance1));
        }

        [Test]
        public void EqualsIsFalseForDifferentUrls()
        {
            var instance1 = new AlertUrl { Url = "http://a.com/b" };
            var instance2 = new AlertUrl { Url = "https://beee.org/ceee" };

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance2, Is.Not.EqualTo(instance1));
        }
    }
}
