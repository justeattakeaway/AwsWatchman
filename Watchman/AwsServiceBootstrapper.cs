using System;
using Amazon.AutoScaling.Model;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.Lambda.Model;
using Amazon.RDS.Model;
using Amazon.StepFunctions.Model;
using StructureMap;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.AutoScaling;
using Watchman.AwsResources.Services.Elb;
using Watchman.AwsResources.Services.Kinesis;
using Watchman.AwsResources.Services.Lambda;
using Watchman.AwsResources.Services.Rds;
using Watchman.AwsResources.Services.StepFunction;
using Watchman.AwsResources.Services.VpcSubnet;
using Watchman.Configuration;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Subnet = Amazon.EC2.Model.Subnet;

namespace Watchman
{
    public static class AwsServiceBootstrapper
    {
        public static void Configure(IProfileRegistry registry)
        {
            AddService<DBInstance, RdsSource, RdsAlarmDataProvider, RdsAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapRds
                );

            AddService
                <AutoScalingGroup, AutoScalingGroupSource, AutoScalingGroupAlarmDataProvider,
                    AutoScalingGroupAlarmDataProvider>(
                        registry, WatchmanServiceConfigurationMapper.MapAutoScaling
                );

            AddService<Subnet, VpcSubnetSource, VpcSubnetAlarmDataProvider, VpcSubnetAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapVpcSubnet
                );

            AddService<FunctionConfiguration, LambdaSource, LambdaAlarmDataProvider, LambdaAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapLambda
                );

            AddService<LoadBalancerDescription, ElbSource, ElbAlarmDataProvider, ElbAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapElb
                );

            AddService<KinesisStreamData, KinesisStreamSource, KinesisStreamAlarmDataProvider, KinesisStreamAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapStream
            );

            AddService<StateMachineListItem, StepFunctionSource, StepFunctionAlarmDataProvider, StepFunctionAlarmDataProvider>(
                registry, WatchmanServiceConfigurationMapper.MapStepFunction
            );
        }

        private static void AddService<TServiceModel, TSource, TDimensionProvider, TAttributeProvider>(
            IProfileRegistry registry,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration> mapper)
            where TServiceModel : class
            where TSource : IResourceSource<TServiceModel>
            where TDimensionProvider : IAlarmDimensionProvider<TServiceModel>
            where TAttributeProvider : IResourceAttributesProvider<TServiceModel>
        {
            registry.For<IResourceSource<TServiceModel>>().Use<TSource>();
            registry.For<IAlarmDimensionProvider<TServiceModel>>().Use<TDimensionProvider>();
            registry.For<IResourceAttributesProvider<TServiceModel>>().Use<TAttributeProvider>();

            registry.For<IServiceAlarmTasks<TServiceModel>>()
                .Use<ServiceAlarmTasks<TServiceModel>>()
                .Ctor<Func<WatchmanConfiguration, WatchmanServiceConfiguration>>()
                .Is(mapper);

            registry.For<IServiceAlarmTasks>().Use(ctx => ctx.GetInstance<IServiceAlarmTasks<TServiceModel>>());
        }
    }
}
