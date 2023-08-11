using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using NSubstitute;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Tests.Fakes;
using Watchman.Tests.IoC;

namespace Watchman.Tests.Alb
{
    class AlbTestSetupBuilder
    {
        private readonly List<LoadBalancer> _loadBalancers = new List<LoadBalancer>();
        private readonly Dictionary<string, AlarmValues> _overrides = new Dictionary<string, AlarmValues>();
        private readonly string _configurationName;
        private readonly string _configurationSuffix;

        private string _pattern;

        public AlbTestSetupBuilder()
        {
            _configurationName = "test";
            _configurationSuffix = "group-suffix";
            _pattern = "loadBalancer";
        }

        public AlbTestSetupBuilder WithPattern(string pattern)
        {
            _pattern = pattern;
            return this;
        }

        public AlbTestSetupBuilder WithOverride(string alarmName, AlarmValues alarmValues)
        {
            _overrides.Add(alarmName, alarmValues);
            return this;
        }

        public AlbTestSetupBuilder WithLoadBalancer(string name, string arn)
        {
            _loadBalancers.Add(new LoadBalancer
            {
                LoadBalancerName = name,
                LoadBalancerArn = arn,
                Type = LoadBalancerTypeEnum.Application
            });

            return this;
        }

        public async Task<AlbTestSetupData> Build()
        {
            if (!_loadBalancers.Any())
            {
                WithDefaultLoadBalancer();
            }

            var config = ConfigHelper.CreateBasicConfiguration(_configurationName, _configurationSuffix,
                new AlertingGroupServices()
                {
                    Alb = new AwsServiceAlarms<ResourceConfig>()
                    {
                        Resources = new List<ResourceThresholds<ResourceConfig>>()
                        {
                            new ResourceThresholds<ResourceConfig>()
                            {
                                Pattern = _pattern
                            }
                        },
                        Values = _overrides
                    }
                });

            var cloudFormation = new FakeCloudFormation();
            var ioc = new TestingIocBootstrapper()
                .WithCloudFormation(cloudFormation.Instance)
                .WithConfig(config);

            ioc.GetMock<IAmazonElasticLoadBalancingV2>()
                .DescribeLoadBalancersAsync(Arg.Any<DescribeLoadBalancersRequest>(), Arg.Any<CancellationToken>())
                .Returns(new DescribeLoadBalancersResponse
                {
                    LoadBalancers = _loadBalancers
                });

            var sut = ioc.Get<AlarmLoaderAndGenerator>();
            await sut.LoadAndGenerateAlarms(RunMode.GenerateAlarms);

            var alarms = cloudFormation
                .Stack($"Watchman-{_configurationName}")
                ?.Resources
                ?.Where(x => x.Value.Type.Equals("AWS::CloudWatch::Alarm", StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Value)
                .ToList();

            return new AlbTestSetupData
            {
                ConfigurationSuffix = _configurationSuffix,
                FakeCloudFormation = cloudFormation,
                LoadBalancers = _loadBalancers,
                Alarms = alarms
            };
        }

        private void WithDefaultLoadBalancer()
        {
            _loadBalancers.Add(new LoadBalancer
            {
                LoadBalancerName = "loadBalancer-1",
                LoadBalancerArn = "loadbalancer/loadBalancer-1",
                Type = LoadBalancerTypeEnum.Application
            });
        }
    }

    class AlbTestSetupData
    {
        public FakeCloudFormation FakeCloudFormation { get; set; }
        public List<Resource> Alarms { get; set; }
        public List<LoadBalancer> LoadBalancers { get; set; }
        public string ConfigurationSuffix { get; set; }
    }
}
