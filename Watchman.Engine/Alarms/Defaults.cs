using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Watchman.Configuration;

namespace Watchman.Engine.Alarms
{
    /// <summary>
    ///  Alarms which will be applied to each service
    ///  The threshold can be overridden by the service or resource definition
    /// </summary>
    public static class Defaults
    {
        public static IList<AlarmDefinition> Rds = new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                Name = "FreeStorageSpaceLow",
                Metric = "FreeStorageSpace",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    SourceAttribute = "AllocatedStorage",
                    ThresholdType = ThresholdType.PercentageOf,
                    Value = 30
                },
                DimensionNames = new[] {"DBInstanceIdentifier"},
                ComparisonOperator = ComparisonOperator.LessThanThreshold,
                Statistic = Statistic.Minimum,
                Namespace = AwsNamespace.Rds
            },
            new AlarmDefinition
            {
                Name = "CPUUtilizationHigh",
                Metric = "CPUUtilization",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 5,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 60
                },
                DimensionNames = new[] {"DBInstanceIdentifier"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Rds
            },
            new AlarmDefinition
            {
                Name = "DatabaseConnectionsHigh",
                Metric = "DatabaseConnections",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 200
                },
                DimensionNames = new[] {"DBInstanceIdentifier"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Rds
            }
        };

        public static IList<AlarmDefinition> AutoScaling = new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                Name = "CPUCreditBalanceLow",
                Metric = "CPUCreditBalance",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 0.2
                },
                DimensionNames = new[] {"AutoScalingGroupName"},
                ComparisonOperator = ComparisonOperator.LessThanThreshold,
                Statistic = Statistic.Minimum,
                Namespace = AwsNamespace.Ec2
            },
            new AlarmDefinition
            {
                Name = "CPUUtilizationHigh",
                Metric = "CPUUtilization",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 2,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 90
                },
                DimensionNames = new[] {"AutoScalingGroupName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Average,
                Namespace = AwsNamespace.Ec2
            },
            new AlarmDefinition
            {
                Name = "GroupInServiceInstancesLow",
                Metric = "GroupInServiceInstances",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 2,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.PercentageOf,
                    Value = 50,
                    SourceAttribute = "GroupDesiredCapacity"
                },
                DimensionNames = new[] {"AutoScalingGroupName"},
                ComparisonOperator = ComparisonOperator.LessThanThreshold,
                Statistic = Statistic.Minimum,
                Namespace = AwsNamespace.AutoScaling
            }
        };

        public static IList<AlarmDefinition> Lambda = new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                Name = "ErrorsHigh",
                Metric = "Errors",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 3
                },
                DimensionNames = new[] {"FunctionName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Lambda
            },
            new AlarmDefinition
            {
                Name = "DurationHigh",
                Metric = "Duration",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.PercentageOf,
                    Value = 50,
                    SourceAttribute = "Timeout"
                },
                DimensionNames = new[] {"FunctionName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Lambda
            },
            new AlarmDefinition
            {
                Name = "ThrottlesHigh",
                Metric = "Throttles",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 5
                },
                DimensionNames = new[] {"FunctionName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Lambda
            }
       };

        public static IList<AlarmDefinition> VpcSubnets = new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                Name = "IpAddressesRemainingLow",
                Metric = "VPC_AvailableIpAddresses",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 4,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.PercentageOf,
                    SourceAttribute = "NumberOfIpAddresses",
                    Value = 30
                },
                DimensionNames = new[] {"Subnet"},
                ComparisonOperator = ComparisonOperator.LessThanOrEqualToThreshold,
                Statistic = Statistic.Minimum,
                Namespace = "JUSTEAT/PlatformLimits",
                AlertOnInsufficientData = true
            }
       };

        public static IList<AlarmDefinition> Elb = new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                Name = "Elb5xxErrorsHigh",
                Metric = "HTTPCode_ELB_5XX",
                Period = TimeSpan.FromMinutes(1),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 50
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Sum,
                Namespace = AwsNamespace.Elb
            },
            new AlarmDefinition
            {
                Name = "Http5xxErrorsHigh",
                Metric = "HTTPCode_Backend_5XX",
                Period = TimeSpan.FromMinutes(1),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 500
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Sum,
                Namespace = AwsNamespace.Elb
            },
            new AlarmDefinition
            {
                Name = "SurgeQueueLengthHigh",
                Metric = "SurgeQueueLength",
                Period = TimeSpan.FromMinutes(1),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 200
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Maximum,
                Namespace = AwsNamespace.Elb
            },
            new AlarmDefinition
            {
                Name = "SpilloverCountHigh",
                Metric = "SpilloverCount",
                Period = TimeSpan.FromMinutes(1),
                EvaluationPeriods = 1,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 10
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Sum,
                Namespace = AwsNamespace.Elb
            },
            new AlarmDefinition
            {
                Name = "LatencyHigh",
                Metric = "Latency",
                Period = TimeSpan.FromMinutes(1),
                EvaluationPeriods = 4,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 0.2
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                // This should be probably a percentile statistic once that is supported via cloudformation
                Statistic = Statistic.Average,
                Namespace = AwsNamespace.Elb
            },
             new AlarmDefinition
            {
                Name = "UnHealthyHostCountHigh",
                Metric = "UnHealthyHostCount",
                Period = TimeSpan.FromMinutes(5),
                EvaluationPeriods = 4,
                Threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = 1
                },
                DimensionNames = new[] {"LoadBalancerName"},
                ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                Statistic = Statistic.Average,
                Namespace = AwsNamespace.Elb
            }
       };
    }
}
