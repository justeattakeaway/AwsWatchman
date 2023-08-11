using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources.Services.StepFunction;

namespace Watchman.AwsResources.Tests.Services.StepFunction
{
    [TestFixture]
    public class StepFunctionSourceTests
    {
        private StepFunctionSource _source;
        private ListStateMachinesResponse _firstPage;
        private ListStateMachinesResponse _secondPage;
        private ListStateMachinesResponse _thirdPage;

        [SetUp]
        public void SetUp()
        {
            _firstPage = new ListStateMachinesResponse
            {
                StateMachines = new List<StateMachineListItem>
                {
                    new StateMachineListItem
                    {
                        CreationDate = DateTime.UtcNow,
                        Name = "MyStepFunction1",
                        StateMachineArn = "arn:aws:states:eu-west-1:12345678:stateMachine:MyStepFunction1"
                    }
                },
                NextToken = "token-1"
            };
            _secondPage = new ListStateMachinesResponse
            {
                StateMachines = new List<StateMachineListItem>
                {
                    new StateMachineListItem
                    {
                        CreationDate = DateTime.UtcNow,
                        Name = "MyStepFunction2",
                        StateMachineArn = "arn:aws:states:eu-west-1:12345678:stateMachine:MyStepFunction2"
                    }
                },
                NextToken = "token-2"
            };
            _thirdPage = new ListStateMachinesResponse
            {
                StateMachines = new List<StateMachineListItem>
                {
                    new StateMachineListItem
                    {
                        CreationDate = DateTime.UtcNow,
                        Name = "MyStepFunction3",
                        StateMachineArn = "arn:aws:states:eu-west-1:12345678:stateMachine:MyStepFunction3"
                    }
                },
                NextToken = null
            };

            var stepFunctionsClient = Substitute.For<IAmazonStepFunctions>();
            stepFunctionsClient.ListStateMachinesAsync(
                Arg.Is<ListStateMachinesRequest>(r => r.NextToken == null),
                Arg.Any<CancellationToken>())
                .Returns(_firstPage);

            stepFunctionsClient.ListStateMachinesAsync(
                    Arg.Is<ListStateMachinesRequest>(r => r.NextToken == "token-1"),
                    Arg.Any<CancellationToken>())
                .Returns(_secondPage);

            stepFunctionsClient.ListStateMachinesAsync(
                    Arg.Is<ListStateMachinesRequest>(r => r.NextToken == "token-2"),
                    Arg.Any<CancellationToken>())
                .Returns(_thirdPage);

            _source = new StepFunctionSource(stepFunctionsClient);
        }

        [Test]
        public async Task GetResourceNamesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // Arrange

            // Act
            var result = await _source.GetResourceNamesAsync();

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.StateMachines.Single().Name));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.StateMachines.Single().Name));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.StateMachines.Single().Name));
        }

        [Test]
        public async Task GetResourceNamesAsync_SinglePage_FetchedAndReturned()
        {
            // Arrange
            _firstPage.NextToken = null;

            // Act
            var result = await _source.GetResourceNamesAsync();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First(), Is.EqualTo(_firstPage.StateMachines.Single().Name));
        }

        [Test]
        public async Task GetResourceNamesAsync_EmptyResult_EmptyListReturned()
        {
            // Arrange
            _firstPage.NextToken = null;
            _firstPage.StateMachines = new List<StateMachineListItem>();

            // Act
            var result = await _source.GetResourceNamesAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // Arrange

            // Act
            var secondResourceName = _secondPage.StateMachines.First().Name;
            var result = await _source.GetResourceAsync(secondResourceName);

            // Assert
            Assert.That(result, Is.InstanceOf<StateMachineListItem>());
            Assert.That(result.Name, Is.EqualTo(secondResourceName));
        }
    }
}
