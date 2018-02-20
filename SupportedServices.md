# Configuration of other service types

## Alerting group

One or more alerting groups should be defined in the config folder that you supply to watchman.

```
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

```
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

# Resource types

The following services are supported

- `Rds`
- `AutoScaling`
- `Lambda`
- `Kinesis`
- `Elb`
- `StepFunction`
- `DynamoDb` (in-progress migration from the existing non-cloudformation mechanism, limited functionality - indexes are not yet monitored.)
- `VpcSubnet` (this is a custom service using JUST EAT custom metrics)

## Alarm names and default thresholds

For each resource each of the default alarms will be applied. See [alarm definitions](Watchman.Engine/Alarms/Defaults.cs) for more details of periods etc.

### Rds

- `FreeStorageSpaceLow`: 30 (%)
- `CPUUtilizationHigh`: 60 (%)
- `DatabaseConnectionsHigh`: 200 (count)

### AutoScaling
#### Values

- `CPUCreditBalanceLow`: 0.2 (credit balance)
- `CPUUtilizationHigh`: 90 (%)
- `GroupInServiceInstancesLow`: 50 (% of desired)

#### Options
- `InstanceCountIncreaseDelayMinutes` Use to delay increasing the minimum threshold when the instance count increases (e.g. when scaling). The value used it then obtained from CloudWatch - using the minimum of `GroupDesiredCapacity` over the time period specified.

### Lambda

- `ErrorsHigh`: 3 (count)
- `DurationHigh`: 50 (% of defined Timeout)
- `ThrottlesHigh`: 5 (count)

### Kinesis

- `ReadProvisionedThroughputExceededHigh`: 1 (count)
- `WriteProvisionedThroughputExceededHigh`: 1 (count)

### StepFunction

- `ExecutionsFailedHigh`: 1 (count)

### DynamoDb

- `ConsumedReadCapacityUnitsHigh`: 80 (%)
- `ConsumedWriteCapacityUnitsHigh`: 80 (%)
- `ThrottledRequestsHigh`: 2 

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

## Full example

```
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
        		}
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
	    }
    }
}
```
