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
using Watchman.AwsResources.Services.Sqs;
using Watchman.AwsResources.Services.StepFunction;
using Watchman.AwsResources.Services.VpcSubnet;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Sqs;
using Subnet = Amazon.EC2.Model.Subnet;

namespace Watchman.IoC
{
    public class AwsServiceRegistry : Registry
    {
        public AwsServiceRegistry()
        {

            AddService<DBInstance, RdsSource, RdsAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapRds
                );

            AddService
                <AutoScalingGroup, AutoScalingGroupSource, AutoScalingGroupAlarmDataProvider, AutoScalingResourceConfig>(
                        WatchmanServiceConfigurationMapper.MapAutoScaling
                );

            AddService<Subnet, VpcSubnetSource, VpcSubnetAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapVpcSubnet
                );

            AddService<FunctionConfiguration, LambdaSource, LambdaAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapLambda
                );

            AddService<LoadBalancerDescription, ElbSource, ElbAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapElb
                );

            AddService<KinesisStreamData, KinesisStreamSource, KinesisStreamAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapStream
            );

            AddService<StateMachineListItem, StepFunctionSource, StepFunctionAlarmDataProvider, ResourceConfig>(
                WatchmanServiceConfigurationMapper.MapStepFunction
            );
            
            AddService<TableDescription, TableDescriptionSource, DynamoDbDataProvider, ResourceConfig, DynamoResourceAlarmGenerator>(
                WatchmanServiceConfigurationMapper.MapDynamoDb);

            AddService<QueueData, QueueSource, QueueDataProvider, SqsResourceConfig, SqsResourceAlarmGenerator>(
                WatchmanServiceConfigurationMapper.MapSqs);

            For<IAlarmDimensionProvider<ErrorQueueData>>().Use<ErrorQueueDataProvider>();
            For<IResourceAttributesProvider<ErrorQueueData, SqsResourceConfig>>().Use<ErrorQueueDataProvider>();
        }

        private void AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig>(
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>> mapper)
            where TServiceModel : class
            where TSource : IResourceSource<TServiceModel>
            where TDataProvider : IAlarmDimensionProvider<TServiceModel>,
            IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>
            where TResourceAlarmConfig : class, IServiceAlarmConfig<TResourceAlarmConfig>, new()

        {
            AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig,
                ResourceAlarmGenerator<TServiceModel, TResourceAlarmConfig>>(
                mapper
            );
        }

        private void AddService<TServiceModel, TSource, TDataProvider, TResourceAlarmConfig, TAlarmBuilder>(
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>> mapper
            )
            where TServiceModel : class
            where TSource : IResourceSource<TServiceModel>
            where TDataProvider : IAlarmDimensionProvider<TServiceModel>,
            IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>
            where TResourceAlarmConfig : class, IServiceAlarmConfig<TResourceAlarmConfig>, new()
            where TAlarmBuilder : IResourceAlarmGenerator<TServiceModel, TResourceAlarmConfig>
        {
            For<IResourceSource<TServiceModel>>().Use<TSource>();
            For<IAlarmDimensionProvider<TServiceModel>>().Use<TDataProvider>();
            For<IResourceAttributesProvider<TServiceModel, TResourceAlarmConfig>>().Use<TDataProvider>();

            For<IServiceAlarmTasks>()
                .Use<ServiceAlarmTasks<TServiceModel, TResourceAlarmConfig>>()
                .Ctor<Func<WatchmanConfiguration, WatchmanServiceConfiguration<TResourceAlarmConfig>>>()
                .Is(mapper);

            For<IResourceAlarmGenerator<TServiceModel, TResourceAlarmConfig>>().Use<TAlarmBuilder>();
        }
    }
}
