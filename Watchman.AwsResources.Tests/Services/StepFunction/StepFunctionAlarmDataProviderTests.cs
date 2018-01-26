using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.StepFunctions.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Watchman.AwsResources.Services.StepFunction;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.StepFunction
{
    [TestFixture]
    public class StepFunctionAlarmDataProviderTests
    {
        private StepFunctionAlarmDataProvider _dataProvider;
        private StateMachineListItem _resource;

        [SetUp]
        public void SetUp()
        {
            _dataProvider = new StepFunctionAlarmDataProvider();
            _resource = new StateMachineListItem
            {
                CreationDate = DateTime.UtcNow,
                Name = "MyStepFunction",
                StateMachineArn = "arn:aws:states:eu-west-1:12345678:stateMachine:MyStepFunction"
            };
        }

        [Test]
        public void GetDimensions_KnownDimensions_ReturnsValue()
        {
            // Arrange

            // Act
            var result = _dataProvider.GetDimensions(_resource, new List<string> {"StateMachineArn"});

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));

            var dimension = result.Single();
            Assert.That(dimension.Name, Is.EqualTo("StateMachineArn"));
            Assert.That(dimension.Value, Is.EqualTo(_resource.StateMachineArn));
        }

        [Test]
        public void GetDimensions_UnknownDimension_ThrowException()
        {
            // Arrange

            // Act
            ActualValueDelegate<List<Dimension>> testDelegate =
                () => _dataProvider.GetDimensions(_resource, new List<string> { "UnknownDimension" });

            // Assert
            Assert.That(testDelegate, Throws.TypeOf<Exception>()
                .With.Message.EqualTo("Unsupported dimension UnknownDimension"));
        }

        [Test]
        public void GetAttribute_UnknownAttribute_ThrowException()
        {
            // Arrange

            // Act
            ActualValueDelegate<Task> testDelegate =
                () => _dataProvider.GetValue(_resource, new ResourceConfig(), "Unknown Attribute");

            // Assert
            Assert.That(testDelegate, Throws.TypeOf<NotImplementedException>());
        }
    }
}
