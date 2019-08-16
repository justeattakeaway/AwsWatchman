using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;
using Amazon.DAX.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Watchman.AwsResources.Services.Dax;

namespace Watchman.AwsResources.Tests.Services.Dax
{
    [TestFixture]
    public class DaxAlarmDataProviderTests
    {
        private Cluster _cluster;
        private DaxAlarmDataProvider _dataProvider;

        [SetUp]
        public void SetUp()
        {
            _cluster = new Cluster
            {
                ClusterName = "my-first-dax-cluster"
            };

            _dataProvider = new DaxAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //act
            var result = _dataProvider.GetDimensions(_cluster, new List<string> { "ClusterId" });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_cluster.ClusterName));
            Assert.That(dim.Name, Is.EqualTo("ClusterId"));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //act
            ActualValueDelegate<List<Dimension>> testDelegate =
                () => _dataProvider.GetDimensions(_cluster, new List<string> { "UnknownDimension" });

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported dimension UnknownDimension"));
        }
    }
}
