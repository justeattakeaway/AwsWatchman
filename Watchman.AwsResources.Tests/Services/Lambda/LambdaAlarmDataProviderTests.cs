using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.Lambda.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Watchman.AwsResources.Services.Lambda;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.Lambda
{
    [TestFixture]
    public class LambdaAlarmDataProviderTests
    {
        private FunctionConfiguration _functionConfig;
        private LambdaAlarmDataProvider _lambdaDataProvider;

        [SetUp]
        public void Setup()
        {
            _functionConfig = new FunctionConfiguration
            {
                FunctionName = "Function Name",
                Timeout = 142
            };

            _lambdaDataProvider = new LambdaAlarmDataProvider();
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            //arange

            //act
            var result = _lambdaDataProvider.GetDimensions(_functionConfig, new List<string> { "FunctionName" });

            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dim = result.Single();
            Assert.That(dim.Value, Is.EqualTo(_functionConfig.FunctionName));
            Assert.That(dim.Name, Is.EqualTo("FunctionName"));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<List<Dimension>> testDelegate =
                () => _lambdaDataProvider.GetDimensions(_functionConfig, new List<string> { "UnknownDimension" });

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported dimension UnknownDimension"));
        }

        [Test]
        public async Task GetAttribute_KnownAttribute_ReturnsValue()
        {
            //arange

            //act
            var result = await _lambdaDataProvider.GetValue(_functionConfig, new ResourceConfig(), "Timeout");

            //assert
            var expected = _functionConfig.Timeout * 1000;
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            //arange

            //act
            ActualValueDelegate<Task> testDelegate =
                () => _lambdaDataProvider.GetValue(_functionConfig, new ResourceConfig(), "Unknown Attribute");

            //assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported Lambda property name"));
        }
    }
}
