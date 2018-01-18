using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.ElasticLoadBalancing.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.Elb;

namespace Watchman.AwsResources.Tests.Services.Elb
{
    [TestFixture]
    public class ElbAlarmDataProviderTests
    {
        private LoadBalancerDescription _elbDescription;
        private ElbAlarmDataProvider _elbDataProvider;

        [SetUp]
        public void Setup()
        {
            _elbDescription = new LoadBalancerDescription
            {
                LoadBalancerName = "LoadBalancer Name"
            };

            _elbDataProvider = new ElbAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange
            const string dimName = "LoadBalancerName";

            //act
            var result = _elbDataProvider.GetDimensions(_elbDescription, null, new List<string> { dimName });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_elbDescription.LoadBalancerName));
            Assert.That(dim.Name, Is.EqualTo(dimName));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange
            const string dimName = "UnknownDimension";

            //act

            //assert
            var ex = Assert.Throws<Exception>(() => _elbDataProvider.GetDimensions(_elbDescription, null, new List<string> { dimName }));
            Assert.That(ex.Message, Is.EqualTo($"Unsupported dimension {dimName}"));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act

            //assert
            Assert.Throws<NotImplementedException>(() => _elbDataProvider.GetValue(_elbDescription, "SomeAttribute"));
        }
    }
}
