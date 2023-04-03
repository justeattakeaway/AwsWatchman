# Configuration of other service types

## Alerting group

One or more alerting groups should be defined in the config folder that you supply to watchman.

```json
{
  "Name": "TEST",
  "AlarmNameSuffix": "TestingGroup",
  "Targets": [
    { "Email": "email@just-eat.com" }
  ],
  "Services": {
      "ServiceIdentifier": { /* service */ }
  }
}
```

## Service definition

Configuration for the additional services is added to the alerting group under the "Services" property. (note that configuration for Dynamo and SQS requires a different configuration format - see existing documentation).

They all require an object in this format:

```json
{
    // [optional] defines default thresholds and evaluation periods for the alerting group
    "Values": {
          "AlarmName": 31
      },

    // [optional] specific options for this service type
    "Options": {
        "InstanceCountIncreaseDelayMinutes": 10
    },

      // Resources for this service type
    "Resources": [
        // regular expression match of resource name
        {
               "Pattern": "^.*$",

               // [optional] override the thresholds defined elsewhere
            "Values": {
                "AlarmName": 11
            },

            // [optional] override specific options for this service type
            "Options": {
                "InstanceCountIncreaseDelayMinutes": 5
            },

            // [optional]
            "Description": "extra custom text for alarm description"
         },

         // exact match of resource name
         {
               "Name": "Resource"
         },

         // or
         "ResourceName"
     ]
}
```

## Overriding threshold and other default attributes

As in the above example, the threshold can be overriden by including the following (at either resource or service level):

```json
  "Values": {
     "AlarmName": 11
  }
```

An alarm can be disabled using:

```json
  "Values": {
     "AlarmName": false
  }
```

Multiple attributes can be overridden if an object is specified:

- `Threshold`: threshold (will be either an absolute value or a percentage - see below)
- `EvaluationPeriods`: number of periods for which the threshold must be breached, in order to trigger the alarm
- `Statistic`: Can be "Average", "Maximum", "Minimum", "SampleCount" or "Sum". NB If ExtendedStatistic is defined then it overrides Statistic.
- `ExtendedStatistic`: instead of the default statistic (e.g. average, max, etc.) use a percentile e.g. "p99"). See [AWS documentation](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/cloudwatch_concepts.html#Percentiles.
- `Enabled`: Whether the alarm is disabled or not. Currently all alarms are default enabled - this might change in future for certain types.

For example:

```json
  "Values": {
     "AlarmName": {
        "Threshold": 11,
        "EvaluationPeriods": 2,
        "ExtendedStatistic": "p99.9"
      }
  }
```

Note that only the values you want to override need to be defined.

## Resource types

The following services are supported

- `Rds`
- `RdsCluster`
- `AutoScaling`
- `Lambda`
- `Kinesis`
- `Elb`
- `Alb`
- `StepFunction`
- `DynamoDb` (new implementation of the existing non-cloudformation mechanism)
- `Sqs` (new implementation of the existing non-cloudformation mechanism)
- `VpcSubnet` (this is a custom service using JUST EAT custom metrics)
- `DAX`
- `CloudFront`

## Alarm names and default thresholds

For each resource each of the default alarms will be applied. See [alarm definitions](Watchman.Engine/Alarms/Defaults.cs) for more details of periods etc.

### Rds

- `FreeStorageSpaceLow`: 30 (%)
- `CPUUtilizationHigh`: 60 (%)
- `DatabaseConnectionsHigh`: 200 (count)
- `CPUSurplusCreditsChargedHigh`: Disabled by default. Enable if a t3.* instance type is used. By default will go into alarm state when any credits are charged for (i.e. the spent surplus credits exceed the maximum number of credits that the instance can earn in a 24-hour period)

### RdsCluster

- `CPUUtilizationHigh`: 60 (%)
- `(Select|Insert|Update|Delete)LatencyHigh`: Disabled by default. These metrics are for Aurora Serverless cluster only. The default is 300 ms maximum latency over 1 minute period.
- `DatabaseConnectionsHigh`: 200 (count)
- `FreeStorageSpaceLow`: 30 (%)

### AutoScaling

#### Values

- `CPUCreditBalanceLow`: 0.2 (credit balance)
- `CPUUtilizationHigh`: 90 (%)
- `GroupInServiceInstancesLow`: 50 (% of desired)

#### Options

- `InstanceCountIncreaseDelayMinutes` Use to delay increasing the minimum threshold when the instance count increases (e.g. when scaling). The value used it then obtained from CloudWatch - using the minimum of `GroupDesiredCapacity` over the time period specified. Note that if CloudWatch metrics are not present then the current Desired capacity is used, as it better to have a more sensitive alarm than none.

#### Important notes

When MinimumInstanceCount < MaximumInstanceCount * 50%, the default `GroupInServiceInstancesLow` (50% of desired instance count) can trigger false alerts on scaling up

Let's take a look at scaling up from 2 to 6 instances (2 is less than 6 * 50% = 3). The alert threshold is updated from 1 to 3 in less than a minute after scaling up gets triggered. Then, this new threshold is compared with two GroupInServiceInstances datapoints which were consequently captured for the previous 10 minutes (each datapoint represents the minimum value in a 5-minute interval). There were 2 instances before the moment scale up was triggered, so a false alert gets triggered because 2 (instance count) < 3 (new threshold).

When MinimumInstanceCount < MaximumInstanceCount * 50%, using `InstanceCountIncreaseDelayMinutes` might fix false scaling up alerts but instead introduces false scaling down alerts

Let's suppose we are scaling down from 6 to 2 instances and `InstanceCountIncreaseDelayMinutes` is set to 15 minutes. The alert threshold is updated from 3 to 1 with 15-minute delay after scaling down gets triggered. Instances finish scaling down before the threshold is updated and a false alert gets triggered.

To solve these problems try either of the following:

- Lower the `GroupInServiceInstancesLow` threshold
- Adjust MinimumInstanceCount/MaximumInstanceCount so that MinimumInstanceCount is greater or equal to MaximumInstanceCount * 50% (e.g. 2/4, 3/6, etc)

### Lambda

- `ErrorsHigh`: 3 (count)
- `DurationHigh`: 50 (% of defined Timeout)
- `ThrottlesHigh`: 5 (count)
- `InvocationsLow`: Disabled by default. If enabled it checks the function was executed at least once in a day.
- `InvocationsHigh`: Disabled by default. If enabled it checks the function was executed at most x in a day.

### Kinesis

- `ReadProvisionedThroughputExceededHigh`: 1 (count)
- `WriteProvisionedThroughputExceededHigh`: 1 (count)

### StepFunction

- `ExecutionsFailedHigh`: 1 (count)

### DynamoDB

- `ConsumedReadCapacityUnitsHigh`: 80 (% of provisioned)
- `ConsumedWriteCapacityUnitsHigh`: 80 (% of provisioned)
- `ReadThrottleEventsHigh`: 2
- `WriteThrottleEventsHigh`: 2
- `GsiConsumedReadCapacityUnitsHigh`: 80 (%)
- `GsiConsumedWriteCapacityUnitsHigh`: 80 (%)
- `GsiReadThrottleEventsHigh`: 2
- `GsiWriteThrottleEventsHigh`: 2

#### Options

- `MonitorWrites` Shorthand can be used to disable all the write alarms. Default is `true`.
- `ThresholdIsAbsolute` If this value is true, the `Consumed` metrics above will use this value as an absolute, instead of the value calculated as a percentage of the provisioned capacity. The value is measured over a period of 60 seconds, so if you want a threshold on WCU / RCU, multiply your desired value by 60. Example: Desired alarm on RCU of 200, so threshold over 60 seconds is `12000`. Default value is `false`.

### Sqs

Sqs will mark any queue as an error queue if it ends with "_error"

- `NumberOfVisibleMessages`: 100
- `AgeOfOldestMessage`: 600 (seconds)
- `NumberOfVisibleMessages_Error`: 10
- `AgeOfOldestMessage_Error`: 600 (seconds)

#### Options

- `IncludeErrorQueues`: Enable special handling of _error queues matching the primary queue name (default `true`)

### VpcSubnets

- `IpAddressesRemainingLow` 30 (% of subnet allocation)

### Elb

Note that using the defaults here for all alarms is probably not that useful.

- `Elb5xxErrorsHigh`: 50 (count)
- `Http5xxErrorsHigh`: 500 (count)
- `SurgeQueueLengthHigh`: 200 (count)
- `SpilloverCountHigh`: 10 (count)
- `LatencyHigh`: 0.5 (average in seconds)
- `UnHealthyHostCountHigh`: 1 (count)

### Alb

- `5xxErrorsHigh`: 10 (count)
- `Target5xxErrorsHigh`: 10 (count)
- `RejectedConnectionCountHigh`: 10 (count)
- `TargetResponseTimeHigh`: 2 (seconds) compared against `p99`

### DAX

- `CPUUtilizationHigh`: 60 (%)

### Cloudfront

- `4xxErrorRate`: 10 errors in 5 minutes

## Full example

```json
{
    "Name": "TEST",
    "AlarmNameSuffix": "TestingGroup",
    "Targets": [
        { "Email": "email@just-eat.com" }
    ],
    "Services": {
        "Rds": {
            "Resources": [{
                "Pattern": "allorest",
                "Thresholds": {
                    "FreeStorageSpaceLow": 20
                },
                "Description": "Storage space is low, perform some action"
            },{
                "Name": "something"
            },{
                "Name": "another"
            }],
            "Values": {
                  "FreeStorageSpaceLow": 31
              }
        }
        "Lambda": {
              "Resources": [
                  {"Pattern": "api-"}
              ],
              "Values": {
                  "ErrorsHigh": 10
              }
        },
        "CloudFront": {
            "Resources": [
                {"Pattern": "my-distribution-id"},
                {"Pattern": "my-dist-overridden-stats",
                "Values": {
                    "4xxErrorRate": {
                        "EvaluationPeriods": 5,
                        "Threshold": 10
                    }
                }}
            ]
        }
    }
}
```
