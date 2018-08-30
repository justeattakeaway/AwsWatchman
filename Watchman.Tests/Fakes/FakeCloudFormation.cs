using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<string, FakeStack> _submitted = new Dictionary<string, FakeStack>();

        private Mock<IAmazonCloudFormation> fake = new Mock<IAmazonCloudFormation>();

        public IAmazonCloudFormation Instance => fake.Object;

        public FakeCloudFormation()
        {
            fake.Setup(x => x.CreateStackAsync(It.IsAny<CreateStackRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback((CreateStackRequest req, CancellationToken token) =>
                {
                    _submitted[req.StackName] = new FakeStack()
                    {
                        LastOperation = LastOperation.Create,
                        StackJson = req.TemplateBody,
                        StackName = req.StackName
                    };
                })
                .ReturnsAsync(new CreateStackResponse());

            fake.Setup(x => x.UpdateStackAsync(It.IsAny<UpdateStackRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback((UpdateStackRequest req, CancellationToken token) =>
                {
                    _submitted[req.StackName] = new FakeStack()
                    {
                        LastOperation = LastOperation.Update,
                        StackJson = req.TemplateBody,
                        StackName = req.StackName
                    };
                })
                .ReturnsAsync(new UpdateStackResponse());

            fake.Setup(x => x.ListStacksAsync(It.IsAny<ListStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new ListStacksResponse()
                {
                    StackSummaries = _submitted
                        .Select(s => new StackSummary()
                        {
                            StackName = s.Key,
                            StackStatus = s.Value.LastOperation == LastOperation.Create
                                ? StackStatus.CREATE_COMPLETE
                                : StackStatus.UPDATE_COMPLETE
                        }).ToList()
                });

            fake.Setup(x => x.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DescribeStacksRequest req, CancellationToken _) => new DescribeStacksResponse()
                {
                    Stacks = _submitted
                        .Select(s => new Stack()
                        {
                            StackName = s.Key,
                            StackStatus = s.Value.LastOperation == LastOperation.Create
                                ? StackStatus.CREATE_COMPLETE
                                : StackStatus.UPDATE_COMPLETE
                        }).ToList()
                });
        }

        public bool StackWasDeployed(string name) => _submitted.ContainsKey(name);

        public int StacksDeployed => _submitted.Count;

        public Template Stack(string name)
        {
            if (!_submitted.ContainsKey(name))
            {
                return null;
            }

            return _submitted[name].Stack;
        }

        enum LastOperation
        {
            Create,
            Update
        }

        class FakeStack
        {
            public LastOperation LastOperation { get; set; }
            public string StackName { get; set; }
            public string StackJson { get; set; }
            public Template Stack => JsonConvert.DeserializeObject<Template>(StackJson);
        }
    }
}
