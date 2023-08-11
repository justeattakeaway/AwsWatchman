using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using NSubstitute;
using Newtonsoft.Json;

namespace Watchman.Tests.Fakes
{
    class FakeCloudFormation
    {
        private readonly Dictionary<string, FakeStack> _submitted = new Dictionary<string, FakeStack>();

        private IAmazonCloudFormation fake = Substitute.For<IAmazonCloudFormation>();

        public int CallsToListStacks { get; private set; } = 0;
        public IAmazonCloudFormation Instance => fake;

        public FakeCloudFormation()
        {
            fake.CreateStackAsync(Arg.Any<CreateStackRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new CreateStackResponse())
                .AndDoes(x =>
                {
                    var req = x.ArgAt<CreateStackRequest>(0);
                    _submitted[req.StackName] = new FakeStack()
                    {
                        LastOperation = LastOperation.Create,
                        StackJson = req.TemplateBody,
                        StackName = req.StackName
                    };
                });

            fake.UpdateStackAsync(Arg.Any<UpdateStackRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new UpdateStackResponse())
                .AndDoes(x =>
                {
                    var req = x.ArgAt<UpdateStackRequest>(0);
                    _submitted[req.StackName] = new FakeStack()
                    {
                        LastOperation = LastOperation.Update,
                        StackJson = req.TemplateBody,
                        StackName = req.StackName
                    };
                });

            fake.ListStacksAsync(Arg.Any<ListStacksRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new ListStacksResponse()
                {
                    StackSummaries = _submitted
                        .Select(s => new StackSummary()
                        {
                            StackName = s.Key,
                            StackStatus = s.Value.LastOperation == LastOperation.Create
                                ? StackStatus.CREATE_COMPLETE
                                : StackStatus.UPDATE_COMPLETE
                        }).ToList()
                })
                .AndDoes(_ => { CallsToListStacks++;});

            fake.DescribeStacksAsync(Arg.Any<DescribeStacksRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => new DescribeStacksResponse()
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

        public IReadOnlyCollection<(string name, Template template)> Stacks()
        {
            return _submitted
                .Select(s => (s.Key, s.Value.Stack))
                .ToArray();
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
