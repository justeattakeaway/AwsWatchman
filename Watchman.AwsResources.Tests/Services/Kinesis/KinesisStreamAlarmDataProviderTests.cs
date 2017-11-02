using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Kinesis;

namespace Watchman.AwsResources.Tests.Services.Kinesis
{
    [TestFixture]
    public class KinesisStreamAlarmDataProviderTests
    {
        private KinesisStreamData _streamData;
        private KinesisStreamAlarmDataProvider _streamDataProvider;

        [TestFixtureSetUp]
        public void Setup()
        {
            _streamData = new KinesisStreamData
            {
                Name = "Stream Name"
            };

            _streamDataProvider = new KinesisStreamAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange

            //act
            var result = _streamDataProvider.GetDimensions(_streamData, new List<string> { "StreamName" });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_streamData.Name));
            Assert.That(dim.Name, Is.EqualTo("StreamName"));
        }

        [Test]
        [ExpectedException(UserMessage = "Unsupported dimension UnknownDimension")]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange

            //act
            var result = _streamDataProvider.GetDimensions(_streamData, new List<string> { "UnknownDimension" });

            //assert
        }

        [Test]
        [ExpectedException(UserMessage = "Unsupported Lambda property name")]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act
            var result = _streamDataProvider.GetValue(_streamData, "Unknown Attribute");

            //assert
        }
    }
}
