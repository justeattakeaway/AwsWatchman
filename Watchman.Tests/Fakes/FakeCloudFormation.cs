using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Moq;
using Newtonsoft.Json;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Tests.Fakes
{
    class FakeCloudFormation
    {
        private readonly Dictionary<string, string> _submitted = new Dictionary<string, string>();

        private Mock<IAmazonCloudFormation> fake = new Mock<IAmazonCloudFormation>();

        public IAmazonCloudFormation Instance => fake.Object;

        public FakeCloudFormation()
        {
            fake.Setup(x => x.CreateStackAsync(It.IsAny<CreateStackRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback((CreateStackRequest req, CancellationToken token) =>
                {
                    _submitted[req.StackName] = req.TemplateBody;
                })
                .ReturnsAsync(new CreateStackResponse());

            fake.Setup(x => x.ListStacksAsync(It.IsAny<ListStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListStacksResponse()
                {
                    StackSummaries = new List<StackSummary>()
                });

            fake.Setup(x => x.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DescribeStacksRequest req, CancellationToken _) => new DescribeStacksResponse()
                {
                    Stacks = new List<Stack>()
                    {
                        new Stack()
                        {
                            StackName = req.StackName,
                            StackStatus = "CREATE_COMPLETE"
                        }
                    }
                });
        }

        public bool StackWasDeployed(string name) => _submitted.ContainsKey(name);

        public string StackJson(string name) => _submitted[name];

        public int StacksDeployed => _submitted.Count;


        public Template Stack(string name)
        {
            if (!_submitted.ContainsKey(name))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Template>(_submitted[name]);
        }
    }
}
