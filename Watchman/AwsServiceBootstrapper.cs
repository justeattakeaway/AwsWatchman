using System;
using Amazon.AutoScaling.Model;
using Amazon.DynamoDBv2.Model;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.Lambda.Model;
using Amazon.RDS.Model;
using Amazon.StepFunctions.Model;
using StructureMap;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.AutoScaling;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.AwsResources.Services.Elb;
using Watchman.AwsResources.Services.Kinesis;
using Watchman.AwsResources.Services.Lambda;
using Watchman.AwsResources.Services.Rds;
using Watchman.AwsResources.Services.StepFunction;
using Watchman.AwsResources.Services.VpcSubnet;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Dynamo;
using Subnet = Amazon.EC2.Model.Subnet;

namespace Watchman
{
    public static class AwsServiceBootstrapper
    {
        public static void Configure(IProfileRegistry registry)
        {

            AddService<DBInstance, RdsSource, RdsAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapRds
                );

            AddService
                <AutoScalingGroup, AutoScalingGroupSource, AutoScalingGroupAlarmDataProvider, AutoScalingResourceConfig>(
                        registry, WatchmanServiceConfigurationMapper.MapAutoScaling
                );

            AddService<Subnet, VpcSubnetSource, VpcSubnetAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapVpcSubnet
                );

            AddService<FunctionConfiguration, LambdaSource, LambdaAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapLambda
                );

            AddService<LoadBalancerDescription, ElbSource, ElbAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapElb
                );

            AddService<KinesisStreamData, KinesisStreamSource, KinesisStreamAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapStream
            );

            AddService<StateMachineListItem, StepFunctionSource, StepFunctionAlarmDataProvider, ResourceConfig>(
                registry, WatchmanServiceConfigurationMapper.MapStepFunction
            );
            
            AddService<TableDescription, TableDescriptionSource, DynamoDbDataProvider, ResourceConfig, DynamoResourceAlarmGenerator>(
                registry, WatchmanServiceConfigurationMapper.MapDynamoDb);
        }

        private static void AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig>(
            IProfileRegistry registry,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>> mapper)
            where TServiceModel : class
            where TSource : IResourceSource<TServiceModel>
            where TDataProvider : IAlarmDimensionProvider<TServiceModel>,
            IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>
            where TResourceAlarmConfig : class, IServiceAlarmConfig<TResourceAlarmConfig>, new()

        {
            AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig,
                ResourceAlarmGenerator<TServiceModel, TResourceAlarmConfig>>(
                registry, mapper
            );
        }

        private static void AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig, TAlarmBuilder>(
            IProfileRegistry registry,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>> mapper
            )
            where TServiceModel : class
            where TSource : IResourceSource<TServiceModel>
            where TDataProvider : IAlarmDimensionProvider<TServiceModel>,
            IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>
            where TResourceAlarmConfig : class, IServiceAlarmConfig<TResourceAlarmConfig>, new()
            where TAlarmBuilder : IResourceAlarmGenerator<TResourceAlarmConfig>
        {
            registry.For<IResourceSource<TServiceModel>>().Use<TSource>();
            registry.For<IAlarmDimensionProvider<TServiceModel>>().Use<TDataProvider>();
            registry.For<IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>>().Use<TDataProvider>();

            registry.For<IServiceAlarmTasks>()
                .Use<ServiceAlarmTasks<TServiceModel, TResourceAlarmConfig>>()
                .Ctor<Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>>>()
                .Is(mapper);

            registry.For<IResourceAlarmGenerator<TResourceAlarmConfig>>().Use<TAlarmBuilder>();
        }
    }
}
