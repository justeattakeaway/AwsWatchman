using System.Collections.Generic;
using System.Linq;
using Amazon.AutoScaling.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.AutoScaling;

namespace Watchman.AwsResources.Tests.Services.AutoScaling
{
    [TestFixture]
    public class AutoScalingGroupAlarmDataProviderTests
    {
        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange
            var sut = new AutoScalingGroupAlarmDataProvider();

            var autoScalingGroup = new AutoScalingGroup
            {
                AutoScalingGroupName = "ASG Name"
            };

            //act
            var result = sut.GetDimensions(autoScalingGroup, null, new List<string> { "AutoScalingGroupName" });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));
            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(autoScalingGroup.AutoScalingGroupName));
            Assert.That(dim.Name, Is.EqualTo("AutoScalingGroupName"));
        }

        [Test]
        public void GetAttribute_KnownAttribute_ReturnsValue()
        {
            //arange
            var sut = new AutoScalingGroupAlarmDataProvider();

            var autoScalingGroup = new AutoScalingGroup
            {
                DesiredCapacity = 30
            };

            //act
            var result = sut.GetValue(autoScalingGroup, "GroupDesiredCapacity");

            //assert
            Assert.That(result, Is.EqualTo(autoScalingGroup.DesiredCapacity));
        }
    }
}
